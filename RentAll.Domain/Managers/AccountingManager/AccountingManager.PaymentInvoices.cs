using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    const int PRORATE_DAYS = 30;

    int FURNISHED_EXPENSE_COST_CODE = 0;
    int UNFURNISHED_EXPENSE_COST_CODE = 0;
    int SECURITY_DEPOSIT_COST_CODE = 0;
    int SECURITY_DEPOSIT_WAIVER_COST_CODE = 0;
    int DEPARTURE_EXPENSE_COST_CODE = 0;
    int MAID_SERVICE_EXPENSE_COST_CODE = 0;
    int PET_FEE_EXPENSE_COST_CODE = 0;
    int PARKING_EXPENSE_COST_CODE = 0;


    #region Setup
    public async Task<List<LedgerLine>> CreateLedgerLinesForOrganizationIdAsync(Organization organization, DateOnly startDate, DateOnly endDate)
    {
        var lineItems = new List<LedgerLine>();
        var lineNumber = 1;

        var requestedDate = startDate;
        var startDateMonth = startDate.Month;
        var startDateYear = startDate.Year;

        // Partial month = NO if: check-in is 1st, OR (month has 31 days AND check-in is 1st or 2nd)
        var daysInMonth = DateTime.DaysInMonth(startDateYear, requestedDate.Month);
        var isMonthPartial = (requestedDate.Day != 1);
        var days = CalculateNumberOfBillingDays(startDate, endDate);


        // Get the Offices for the Organization
        IEnumerable<Office> offices = await _organizationRepository.GetOfficesByOrganizationIdAsync(organization.OrganizationId);

        foreach (var office in offices)
        {
            var officeLine = $"Office Base Fee ({office.Name}): ({startDate:MM/dd}-{endDate:MM/dd})";
            lineItems.Add(new LedgerLine { LineNumber = lineNumber++, Description = officeLine, Amount = organization.OfficeFee });

            var properties = await _propertyRepository.GetPropertyListByOfficeIdsAsync(office.OrganizationId, Convert.ToString(office.OfficeId));
            var units = properties.Count() - 50;
            switch (properties.Count())
            {
                case <= 2:
                    break;
                case <= 4:
                    lineItems.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"Unit Fee ({units} Units) for {office.Name}", Amount = units * organization.Unit50Fee });
                    break;
                case <= 6:
                    lineItems.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"Unit Fee ({units} Units) for {office.Name}", Amount = units * organization.Unit100Fee });
                    break;
                case <= 8:
                    lineItems.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"Unit Fee ({units} Units) for {office.Name}", Amount = units * organization.Unit200Fee });
                    break;
                default:
                    lineItems.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"Unit Fee ({units} Units) for {office.Name}", Amount = units * organization.Unit500Fee });
                    break;
            }
        }
        await ApplyBillingCostCodesAsync(SystemOrganization, lineItems);
        return lineItems;
    }

    public async Task ApplyBillingCostCodesAsync(Guid organizationId, List<LedgerLine> ledgerLines)
    {
        var costCodes = (await LoadCostCodeByOfficeIdAsync(organizationId, 1)).Values.ToList();
        foreach (var line in ledgerLines)
        {
            var costCode = null as CostCode;
            switch (line.Description)
            {
                case string desc when desc.StartsWith("Office Base", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Office Base"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
                case string desc when desc.StartsWith("Unit Fee", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Unit Fee"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
            }
        }
    }

    public async Task CreateDefaultCostCodeAsync(Guid organizationId, int officeId)
    {
        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        if (office == null)
            return;

        FURNISHED_EXPENSE_COST_CODE = office.FurnishedRentChargeCcId ?? 0;
        UNFURNISHED_EXPENSE_COST_CODE = office.UnfurnishedRentChargeCcId ?? 0;
        SECURITY_DEPOSIT_COST_CODE = office.SecurityDepositCcId ?? 0;
        SECURITY_DEPOSIT_WAIVER_COST_CODE = office.SecurityDepositWaiverCcId ?? 0;
        DEPARTURE_EXPENSE_COST_CODE = office.DepartureFeeCcId ?? 0;
        MAID_SERVICE_EXPENSE_COST_CODE = office.MaidServiceChargeCcId ?? 0;
        PET_FEE_EXPENSE_COST_CODE = office.PetFeeCcId ?? 0;
        PARKING_EXPENSE_COST_CODE = office.ParkingChargeCcId ?? 0;
    }
    #endregion

    #region Invoices
    public async Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId,
        string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser)
    {
        var invoices = new List<Invoice>();
        foreach (var invoiceGuid in invoiceGuids)
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceGuid, organizationId);
            if (invoice == null) throw new Exception("Invalid Invoice");
            invoices.Add(invoice);
        }

        // Order invoices from the oldest to the newest
        invoices = invoices.Where(i => i.IsActive).OrderBy(i => i.InvoiceDate).ToList();

        var availableAmount = amountPaid;
        var pendingPaymentUpdates = new List<(Invoice Invoice, int PaymentLineNumber)>();
        for (var invoiceIndex = 0; invoiceIndex < invoices.Count && availableAmount != 0; invoiceIndex++)
        {
            var invoice = invoices[invoiceIndex];
            var isLastInvoice = invoiceIndex == invoices.Count - 1;
            decimal amountForInvoice;

            if (availableAmount > 0 && !isLastInvoice)
            {
                // For positive multi-invoice runs, fill current due first, then carry remainder.
                var remainingBalance = invoice.TotalAmount - invoice.PaidAmount;
                if (remainingBalance <= 0)
                    continue;

                amountForInvoice = Math.Min(availableAmount, remainingBalance);
            }
            else
            {
                // For single-invoice runs, last invoice in a multi-run, and all negative adjustments:
                // apply as entered so invoice math can naturally go negative/overpaid.
                amountForInvoice = availableAmount;
            }

            if (amountForInvoice == 0)
                continue;

            invoice.PaidAmount += amountForInvoice;
            var maxLineNumber = invoice.LedgerLines.Any() ? invoice.LedgerLines.Max(ll => ll.LineNumber) : 0;
            var paymentLineNumber = maxLineNumber + 1;
            invoice.LedgerLines.Add(new LedgerLine
            {
                InvoiceId = invoice.InvoiceId,
                LineNumber = paymentLineNumber,
                ReservationId = invoice.ReservationId,
                CostCodeId = costCodeId,
                Description = description,
                Amount = amountForInvoice,
                LedgerLineDate = paymentDate,
                CreatedBy = currentUser
            });

            availableAmount -= amountForInvoice;
            pendingPaymentUpdates.Add((invoice, paymentLineNumber));
        }

        var paymentApplications = new List<InvoicePaymentApplication>();
        if (pendingPaymentUpdates.Count > 0)
        {
            var updatedInvoices = await _accountingRepository.UpdateByIdsInTransactionAsync(
                pendingPaymentUpdates.Select(p => p.Invoice).ToList());

            foreach (var (invoice, paymentLineNumber) in pendingPaymentUpdates)
            {
                var updatedInvoice = updatedInvoices.Single(i => i.InvoiceId == invoice.InvoiceId);
                var invoiceIndex = invoices.FindIndex(i => i.InvoiceId == updatedInvoice.InvoiceId);
                if (invoiceIndex >= 0)
                    invoices[invoiceIndex] = updatedInvoice;

                var paymentLedgerLine = updatedInvoice.LedgerLines.Single(l => l.LineNumber == paymentLineNumber);
                paymentApplications.Add(new InvoicePaymentApplication
                {
                    Invoice = updatedInvoice,
                    PaymentLedgerLine = paymentLedgerLine
                });
            }
        }

        var response = new InvoicePayment
        {
            Invoices = invoices,
            PaymentApplications = paymentApplications
        };
        return response;
    }

    public async Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateOnly invoiceDate, DateOnly startDate, DateOnly endDate)
    {
        await CreateDefaultCostCodeAsync(reservation.OrganizationId, reservation.OfficeId);

        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, reservation.OrganizationId);
        var isFurnished = property!.Unfurnished ? false : true;
        var rentalCostCodeId = isFurnished ? FURNISHED_EXPENSE_COST_CODE : UNFURNISHED_EXPENSE_COST_CODE;

        var ledgerLines = GetLedgerLinesByReservationIdAsync(reservation, startDate, endDate, rentalCostCodeId);
        foreach (var ledgerLine in ledgerLines)
            ledgerLine.LedgerLineDate = invoiceDate;

        return ledgerLines;
    }

    public List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation, DateOnly startDate, DateOnly endDate, int rentalCostCodeId)
    {
        var lineItems = new List<LedgerLine>();
        var lineNumber = 1;

        var startDateDay = startDate.Day;
        var startDateMonth = startDate.Month;
        var startDateYear = startDate.Year;

        var endDateDay = endDate.Day;
        var endDateMonth = endDate.Month;
        var endDateYear = endDate.Year;

        var daysInMonth = DateTime.DaysInMonth(startDateYear, startDateMonth);

        var arrivalDate = reservation.ArrivalDate;
        var arrivalDay = reservation.ArrivalDate.Day;
        var arrivalMonth = reservation.ArrivalDate.Month;
        var arrivalYear = reservation.ArrivalDate.Year;

        var departureDate = reservation.DepartureDate;
        var departureDay = reservation.DepartureDate.Day;
        var departureMonth = reservation.DepartureDate.Month;
        var departureYear = reservation.DepartureDate.Year;

        var firstDayOfMonth = new DateOnly(startDateYear, startDateMonth, 1);
        var lastDayOfMonth = new DateOnly(startDateYear, startDateMonth, DateTime.DaysInMonth(startDateYear, startDateMonth));
        var firstDayOfArrivalMonth = new DateOnly(arrivalYear, arrivalMonth, 1);
        var lastDayOfArrivalMonth = new DateOnly(arrivalYear, arrivalMonth, DateTime.DaysInMonth(arrivalYear, arrivalMonth));

        // Partial month = Yes if: check-in is not the 1st, OR (we still have 30 days)
        var daysInArrivalMonth = DateTime.DaysInMonth(arrivalYear, arrivalMonth);
        var isFirstMonthPartial = (arrivalDay != 1 || (daysInArrivalMonth == 31 && arrivalDay > 2));
        var isDepartureMonthYear = endDateMonth == departureMonth && endDateYear == departureYear;
        var isLastDayOfMonth = endDate.Day == lastDayOfMonth.Day;

        // Calculate start day of secondMonth based on whether first month was prorated or not
        // If prorated: billed to end of first month, so second month starts the day after
        // If not prorated: billed for 30 days from check-in
        var secondMonthDate = (reservation.ProrateType == ProrateType.FirstMonth) ? lastDayOfArrivalMonth.AddDays(1) : arrivalDate.AddDays(PRORATE_DAYS);
        var secondMonth = secondMonthDate.Month;
        var secondYear = secondMonthDate.Year;

        var isFirstMonth = startDateMonth == arrivalMonth && startDateYear == arrivalYear;
        var isSecondMonth = startDateMonth == secondMonth;
        var isFirstMonthProrated = reservation.ProrateType == ProrateType.FirstMonth;
        var isFirstMonthLessThan30Days = daysInArrivalMonth < PRORATE_DAYS;
        var isFirstMonthAndFirstMonthPartial = isFirstMonth && isFirstMonthPartial;
        var isSecondMonthFirstMonthPartial = isSecondMonth && isFirstMonthPartial;
        var isProratedMonth = isFirstMonthAndFirstMonthPartial || isSecondMonthFirstMonthPartial;

        // Use end date to hold payments to certain timeframe
        var firstDayOfLastMonth = new DateOnly(departureYear, departureMonth, 1);
        var lastDayOfLastMonth = endDate <= reservation.DepartureDate ? endDate : reservation.DepartureDate;

        // If you're in and out in the same month OR less than 30 days
        if (arrivalMonth == startDateMonth && arrivalYear == startDateYear && departureMonth == startDateMonth && departureYear == startDateYear)
        {
            var days = CalculateNumberOfDays(arrivalDate, departureDate, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
            AddRentalLine(days, reservation, arrivalDate, departureDate, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
            GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
            AddMaidServiceLines(reservation, arrivalDate, departureDate, startDateYear, startDateMonth, lineItems, ref lineNumber);
            return lineItems;
        }

        // FirstMonth, partialFirstMonth & FirstMonthProrated
        if (isFirstMonthAndFirstMonthPartial || (isFirstMonth && reservation.BillingType != BillingType.Monthly && isFirstMonthLessThan30Days))
        {
            var lastDay = reservation.ProrateType == ProrateType.FirstMonth ? lastDayOfMonth : arrivalDate.AddDays(PRORATE_DAYS - 1);
            var days = CalculateNumberOfDays(arrivalDate, lastDay, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
            AddRentalLine(days, reservation, arrivalDate, lastDay, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
            GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
            AddMaidServiceLines(reservation, arrivalDate, lastDay, startDateYear, startDateMonth, lineItems, ref lineNumber);
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, arrivalDate, lastDay, startDateYear, startDateMonth, isProratedMonth, days, lineItems, ref lineNumber);
            return lineItems;
        }

        // SecondMonth, partialFirstMonth & SecondMonthProrated
        if (isSecondMonthFirstMonthPartial || (isSecondMonth && reservation.BillingType != BillingType.Monthly && isFirstMonthLessThan30Days))
        {
            var firstDay = reservation.ProrateType == ProrateType.SecondMonth ? arrivalDate.AddDays(PRORATE_DAYS) : firstDayOfMonth;
            var lastDay = (startDateMonth == departureMonth) ? lastDayOfLastMonth : lastDayOfMonth;
            var days = CalculateNumberOfDays(firstDay, lastDay, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
            AddRentalLine(days, reservation, firstDay, lastDay, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
            AddMaidServiceLines(reservation, firstDay, lastDayOfMonth, startDateYear, startDateMonth, lineItems, ref lineNumber);
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, firstDay, lastDayOfMonth, startDateYear, startDateMonth, isProratedMonth, days, lineItems, ref lineNumber);
            return lineItems;
        }

        // If this is your last month
        if (startDateMonth == departureMonth)
        {
            var days = CalculateNumberOfDays(firstDayOfLastMonth, lastDayOfLastMonth, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
            AddRentalLine(days, reservation, firstDayOfLastMonth, lastDayOfLastMonth, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
            AddMaidServiceLines(reservation, firstDayOfLastMonth, lastDayOfLastMonth, startDateYear, startDateMonth, lineItems, ref lineNumber);
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, firstDayOfLastMonth, lastDayOfLastMonth, startDateYear, startDateMonth, true, days, lineItems, ref lineNumber);
            return lineItems;
        }

        // Otherwise, simply bill for the full month
        var checkoutDays = CalculateNumberOfDays(firstDayOfMonth, lastDayOfMonth, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
        AddRentalLine(checkoutDays, reservation, firstDayOfMonth, lastDayOfMonth, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
        GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
        AddMaidServiceLines(reservation, firstDayOfMonth, lastDayOfMonth, startDateYear, startDateMonth, lineItems, ref lineNumber);
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
            AddExtraFeeLines(extraFeeLine, firstDayOfMonth, lastDayOfMonth, startDateYear, startDateMonth, isProratedMonth, checkoutDays, lineItems, ref lineNumber);
        return lineItems;
    }
    #endregion

    #region Private Methods
    private void GetFirstMonthLines(Reservation reservation, bool isFirstMonth, List<LedgerLine> lines, ref int lineNumber)
    {
        if (!isFirstMonth)
            return;

        if (reservation.DepositType == DepositType.Deposit)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit", Amount = reservation.Deposit, CostCodeId = SECURITY_DEPOSIT_COST_CODE });
        if (reservation.HasPets)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Pet Fee", Amount = reservation.PetFee, CostCodeId = PET_FEE_EXPENSE_COST_CODE });
        if (reservation.DepartureFee >= 0)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Departure Fee", Amount = reservation.DepartureFee, CostCodeId = DEPARTURE_EXPENSE_COST_CODE });

        // We add the one-time fees up front
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
        {
            if (extraFeeLine.FeeFrequency == FrequencyType.OneTime)
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"{extraFeeLine.FeeDescription}", Amount = extraFeeLine.FeeAmount, CostCodeId = extraFeeLine.CostCodeId });
        }
    }

    private void AddRentalLine(int days, Reservation reservation, DateOnly startDate, DateOnly endDate, int daysInMonth,
        bool isDepartureMonthYear, bool isLastDayOfMonth, List<LedgerLine> lines, ref int lineNumber, int costCodeId)
    {

        if (days < daysInMonth && reservation.BillingType == BillingType.Nightly && isDepartureMonthYear) // && isLastDayOfMonth
            endDate = endDate.AddDays(-1);

        var rentLine = $"Rental Fee ({startDate:MM/dd}-{endDate:MM/dd})";
        if (reservation.BillingType == BillingType.Monthly)
        {
            // Days in month (days < days in month) 
            if (days < daysInMonth && days < PRORATE_DAYS)
            {
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = rentLine, Amount = (reservation.BillingRate / PRORATE_DAYS) * days, CostCodeId = costCodeId });
                if (reservation.DepositType == DepositType.SDW)
                    lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit Waiver", Amount = (reservation.Deposit / PRORATE_DAYS) * days, CostCodeId = SECURITY_DEPOSIT_WAIVER_COST_CODE });
            }
            else
            {
                // Full month
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = rentLine, Amount = reservation.BillingRate, CostCodeId = costCodeId });
                if (reservation.DepositType == DepositType.SDW)
                    lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit Waiver", Amount = reservation.Deposit, CostCodeId = SECURITY_DEPOSIT_WAIVER_COST_CODE });
            }
        }
        else
        {
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = rentLine, Amount = days * reservation.BillingRate, CostCodeId = costCodeId });
            if (reservation.DepositType == DepositType.SDW)
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit Waiver", Amount = (reservation.Deposit / PRORATE_DAYS) * days, CostCodeId = SECURITY_DEPOSIT_WAIVER_COST_CODE });
        }


    }

    private void AddMaidServiceLines(Reservation reservation, DateOnly startDate, DateOnly endDate, int requestedYear, int startDateMonth, List<LedgerLine> lines, ref int lineNumber)
    {
        var sDate = reservation.MaidStartDate > startDate ? reservation.MaidStartDate : startDate;
        var dDate = endDate > reservation.DepartureDate.AddDays(-7) ? reservation.DepartureDate : endDate;
        var maidServices = CountMaidServicesInPeriod(reservation, sDate, dDate);

        if (maidServices > 0)
            lines.Add(CreateMaidServiceLedgerLine(maidServices, reservation.MaidServiceFee, lineNumber++, MAID_SERVICE_EXPENSE_COST_CODE));
    }

    static LedgerLine CreateMaidServiceLedgerLine(int visitCount, decimal feePerVisit, int lineNumber, int costCodeId)
        => new()
        {
            LineNumber = lineNumber,
            Description = $"Maid Service ({visitCount} times)",
            Amount = visitCount * feePerVisit,
            CostCodeId = costCodeId
        };

    static int CountMaidServicesInPeriod(Reservation reservation, DateOnly rangeStart, DateOnly rangeEnd)
        => GetMaidServiceOccurrenceDates(reservation, rangeStart, rangeEnd).Count;

    static List<DateOnly> GetMaidServiceOccurrenceDates(Reservation reservation, DateOnly rangeStart, DateOnly rangeEnd)
        => GetScheduledOccurrenceDates(reservation.MaidStartDate, rangeStart, rangeEnd, reservation.Frequency);

    static int CountScheduledOccurrences(DateOnly scheduleStart, DateOnly rangeStart, DateOnly rangeEnd, FrequencyType frequency)
        => GetScheduledOccurrenceDates(scheduleStart, rangeStart, rangeEnd, frequency).Count;

    static List<DateOnly> GetScheduledOccurrenceDates(DateOnly scheduleStart, DateOnly rangeStart, DateOnly rangeEnd, FrequencyType frequency)
    {
        if (rangeStart > rangeEnd)
            return [];

        var dates = new List<DateOnly>();
        switch (frequency)
        {
            case FrequencyType.Daily:
                for (var d = scheduleStart; d <= rangeEnd; d = d.AddDays(1))
                {
                    if (d >= rangeStart)
                        dates.Add(d);
                }
                break;
            case FrequencyType.Weekly:
                for (var d = scheduleStart; d <= rangeEnd; d = d.AddDays(7))
                {
                    if (d >= rangeStart)
                        dates.Add(d);
                }
                break;
            case FrequencyType.EOW:
                for (var d = scheduleStart; d <= rangeEnd; d = d.AddDays(14))
                {
                    if (d >= rangeStart)
                        dates.Add(d);
                }
                break;
            default:
                var monthInterval = GetScheduledMonthInterval(frequency);
                if (monthInterval == 0)
                    break;

                for (var d = scheduleStart; d <= rangeEnd; d = d.AddMonths(monthInterval))
                {
                    if (d >= rangeStart)
                        dates.Add(d);
                }
                break;
        }

        return dates;
    }

    static int GetScheduledMonthInterval(FrequencyType frequency)
        => frequency switch
        {
            FrequencyType.Monthly => 1,
            FrequencyType.Quarterly => 3,
            FrequencyType.BiAnnually => 6,
            FrequencyType.Annually => 12,
            _ => 0
        };

    private void AddExtraFeeLines(ExtraFeeLine extraFeeLine, DateOnly startDate, DateOnly endDate, int requestedYear, int startDateMonth, bool isProratedMonth, int days,
        List<LedgerLine> lines, ref int lineNumber)
    {
        var fees = CountScheduledOccurrences(startDate, startDate, endDate, extraFeeLine.FeeFrequency);

        if (fees > 0)
        {
            var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            if (extraFeeLine.FeeFrequency == FrequencyType.Monthly && isProratedMonth && days < daysInMonth && days < PRORATE_DAYS)
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"{extraFeeLine.FeeDescription}", Amount = (extraFeeLine.FeeAmount / PRORATE_DAYS) * days, CostCodeId = extraFeeLine.CostCodeId });
            else
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"{extraFeeLine.FeeDescription}", Amount = fees * extraFeeLine.FeeAmount, CostCodeId = extraFeeLine.CostCodeId });
        }
    }

    #endregion

    #region Day Calculation Methods
    private static int CalculateNumberOfBillingDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate) return 0;

        return endDate.DayNumber - startDate.DayNumber;
    }

    private static int CalculateNumberOfDays(DateOnly startDate, DateOnly endDate, BillingType billingType, bool isDepartureMonthYear, bool isLastDayOfMonth)
    {
        if (endDate < startDate) return 0;
        if (endDate == startDate) return 1;

        var days = endDate.DayNumber - startDate.DayNumber;
        if (billingType != BillingType.Nightly ||
           (billingType == BillingType.Nightly && !isDepartureMonthYear && isLastDayOfMonth))
            days++;
        return days;
    }
    #endregion
}

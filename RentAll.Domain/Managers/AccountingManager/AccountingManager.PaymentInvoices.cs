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
        await Task.CompletedTask;
        return [];
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
    public async Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser)
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

        var costCodeById = await LoadCostCodeByOfficeIdAsync(reservation.OrganizationId, reservation.OfficeId);
        ApplyMissingDefaultCostCodesFromOfficeCostCodes(costCodeById);

        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, reservation.OrganizationId);
        var isFurnished = property == null || !property.Unfurnished;
        var rentalCostCodeId = isFurnished ? FURNISHED_EXPENSE_COST_CODE : UNFURNISHED_EXPENSE_COST_CODE;

        var ledgerLines = GetLedgerLinesByReservationIdAsync(reservation, startDate, endDate, rentalCostCodeId);
        foreach (var ledgerLine in ledgerLines)
        {
            ledgerLine.LedgerLineDate = invoiceDate;
            ApplyTransactionTypeFromCostCode(ledgerLine, costCodeById);
        }

        return ledgerLines;
    }

    private void ApplyMissingDefaultCostCodesFromOfficeCostCodes(IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        // Same pattern as ApplyBillingCostCodesAsync / escrow COA resolvers:
        // office default first, then match an active office cost code by description.
        SECURITY_DEPOSIT_WAIVER_COST_CODE = ResolveDefaultCostCodeId(
            costCodeById,
            SECURITY_DEPOSIT_WAIVER_COST_CODE,
            descriptionContains: "Security Deposit Waiver");
        if (SECURITY_DEPOSIT_WAIVER_COST_CODE <= 0)
            SECURITY_DEPOSIT_WAIVER_COST_CODE = ResolveDefaultCostCodeId(
                costCodeById,
                0,
                descriptionContains: "Deposit Waiver");

        SECURITY_DEPOSIT_COST_CODE = ResolveDefaultCostCodeId(
            costCodeById,
            SECURITY_DEPOSIT_COST_CODE,
            descriptionContains: "Security Deposit",
            descriptionExcludes: "Waiver");

        DEPARTURE_EXPENSE_COST_CODE = ResolveDefaultCostCodeId(
            costCodeById,
            DEPARTURE_EXPENSE_COST_CODE,
            descriptionContains: "Departure Fee");

        PET_FEE_EXPENSE_COST_CODE = ResolveDefaultCostCodeId(
            costCodeById,
            PET_FEE_EXPENSE_COST_CODE,
            descriptionContains: "Pet Fee");

        MAID_SERVICE_EXPENSE_COST_CODE = ResolveDefaultCostCodeId(
            costCodeById,
            MAID_SERVICE_EXPENSE_COST_CODE,
            descriptionContains: "Maid Service");

        PARKING_EXPENSE_COST_CODE = ResolveDefaultCostCodeId(
            costCodeById,
            PARKING_EXPENSE_COST_CODE,
            descriptionContains: "Parking");
    }

    private static int ResolveDefaultCostCodeId(
        IReadOnlyDictionary<int, CostCode> costCodeById,
        int configuredCostCodeId,
        string descriptionContains,
        string? descriptionExcludes = null)
    {
        if (configuredCostCodeId > 0 && costCodeById.ContainsKey(configuredCostCodeId))
            return configuredCostCodeId;

        var match = costCodeById.Values
            .Where(c => c.IsActive
                && c.Description.Contains(descriptionContains, StringComparison.OrdinalIgnoreCase)
                && (descriptionExcludes == null
                    || !c.Description.Contains(descriptionExcludes, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.CostCodeId)
            .FirstOrDefault();

        return match?.CostCodeId ?? 0;
    }

    private static void ApplyTransactionTypeFromCostCode(LedgerLine ledgerLine, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (ledgerLine.CostCodeId > 0 && costCodeById.TryGetValue(ledgerLine.CostCodeId, out var costCode))
            ledgerLine.TransactionType = costCode.TransactionType;
    }

    public List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation, DateOnly startDate, DateOnly endDate, int rentalCostCodeId)
    {
        var lineItems = new List<LedgerLine>();
        var lineNumber = 1;

        var (billablePeriodStart, billablePeriodEnd) = ResolveBillingPeriodForMonth(reservation, FirstDayOfMonth(startDate));
        if (!HasBillablePreviewPeriod(reservation, billablePeriodStart, billablePeriodEnd))
            return lineItems;

        var startDateDay = startDate.Day;
        var startDateMonth = startDate.Month;
        var startDateYear = startDate.Year;

        var endDateDay = endDate.Day;
        var endDateMonth = endDate.Month;
        var endDateYear = endDate.Year;

        var daysInMonth = DateTime.DaysInMonth(startDateYear, startDateMonth);

        var arrivalDate = ResolveBillingArrivalDate(reservation);
        var arrivalDay = arrivalDate.Day;
        var arrivalMonth = arrivalDate.Month;
        var arrivalYear = arrivalDate.Year;

        var departureDate = ResolveBillingDepartureDate(reservation);
        var departureDay = departureDate.Day;
        var departureMonth = departureDate.Month;
        var departureYear = departureDate.Year;

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
        var lastBillableMonth = ResolveLastBillableMonth(reservation);
        var isLastMonth = startDateMonth == lastBillableMonth.Month && startDateYear == lastBillableMonth.Year;
        var isSecondMonth = startDateMonth == secondMonth;
        var isFirstMonthProrated = reservation.ProrateType == ProrateType.FirstMonth;
        var isFirstMonthLessThan30Days = daysInArrivalMonth < PRORATE_DAYS;
        var isFirstMonthAndFirstMonthPartial = isFirstMonth && isFirstMonthPartial;
        var isSecondMonthFirstMonthPartial = isSecondMonth && isFirstMonthPartial;
        var isProratedMonth = isFirstMonthAndFirstMonthPartial || isSecondMonthFirstMonthPartial;

        // Use end date to hold payments to certain timeframe
        var firstDayOfLastMonth = lastBillableMonth;
        var lastDayOfLastMonth = endDate <= departureDate ? endDate : departureDate;

        // If you're in and out in the same month OR less than 30 days
        if (arrivalMonth == startDateMonth && arrivalYear == startDateYear && departureMonth == startDateMonth && departureYear == startDateYear)
        {
            var days = CalculateNumberOfDays(arrivalDate, departureDate, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
            AddRentalLine(days, reservation, arrivalDate, departureDate, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
            GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
            AddMaidServiceLines(reservation, arrivalDate, departureDate, startDateYear, startDateMonth, lineItems, ref lineNumber);
            AddDepartureFeeIfApplicable(reservation, startDate, lineItems, ref lineNumber);
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
            AddDepartureFeeIfApplicable(reservation, startDate, lineItems, ref lineNumber);
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
            AddDepartureFeeIfApplicable(reservation, startDate, lineItems, ref lineNumber);
            return lineItems;
        }

        // If this is your last month
        if (isLastMonth)
        {
            var days = CalculateNumberOfDays(firstDayOfLastMonth, lastDayOfLastMonth, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
            AddRentalLine(days, reservation, firstDayOfLastMonth, lastDayOfLastMonth, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
            AddMaidServiceLines(reservation, firstDayOfLastMonth, lastDayOfLastMonth, startDateYear, startDateMonth, lineItems, ref lineNumber);
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, firstDayOfLastMonth, lastDayOfLastMonth, startDateYear, startDateMonth, true, days, lineItems, ref lineNumber);
            AddDepartureFeeIfApplicable(reservation, startDate, lineItems, ref lineNumber);
            return lineItems;
        }

        // Otherwise, simply bill for the full month
        var checkoutDays = CalculateNumberOfDays(firstDayOfMonth, lastDayOfMonth, reservation.BillingType, isDepartureMonthYear, isLastDayOfMonth);
        AddRentalLine(checkoutDays, reservation, firstDayOfMonth, lastDayOfMonth, daysInMonth, isDepartureMonthYear, isLastDayOfMonth, lineItems, ref lineNumber, rentalCostCodeId);
        GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
        AddMaidServiceLines(reservation, firstDayOfMonth, lastDayOfMonth, startDateYear, startDateMonth, lineItems, ref lineNumber);
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
            AddExtraFeeLines(extraFeeLine, firstDayOfMonth, lastDayOfMonth, startDateYear, startDateMonth, isProratedMonth, checkoutDays, lineItems, ref lineNumber);
        AddDepartureFeeIfApplicable(reservation, startDate, lineItems, ref lineNumber);
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

        // We add the one-time fees up front
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
        {
            if (extraFeeLine.FeeFrequency == FrequencyType.OneTime)
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"{extraFeeLine.FeeDescription}", Amount = extraFeeLine.FeeAmount, CostCodeId = extraFeeLine.CostCodeId });
        }
    }

    private void AddDepartureFeeIfApplicable(Reservation reservation, DateOnly invoicePeriodStart, List<LedgerLine> lines, ref int lineNumber)
    {
        if (reservation.DepartureFee <= 0)
            return;

        var billingArrivalDate = ResolveBillingArrivalDate(reservation);
        var isFirstMonth = invoicePeriodStart.Month == billingArrivalDate.Month
            && invoicePeriodStart.Year == billingArrivalDate.Year;
        var lastBillableMonth = ResolveLastBillableMonth(reservation);
        var isLastMonth = invoicePeriodStart.Month == lastBillableMonth.Month
            && invoicePeriodStart.Year == lastBillableMonth.Year;

        var shouldCharge = reservation.ReservationType == ReservationType.Platform ? isLastMonth : isFirstMonth;

        if (!shouldCharge)
            return;

        lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Departure Fee", Amount = reservation.DepartureFee, CostCodeId = DEPARTURE_EXPENSE_COST_CODE });
    }

    private void AddRentalLine(int days, Reservation reservation, DateOnly startDate, DateOnly endDate, int daysInMonth, bool isDepartureMonthYear, bool isLastDayOfMonth, List<LedgerLine> lines, ref int lineNumber, int costCodeId)
    {
        if (days <= 0)
            return;

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
        var billingDepartureDate = ResolveBillingDepartureDate(reservation);
        var dDate = endDate > billingDepartureDate.AddDays(-7) ? billingDepartureDate : endDate;
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

    private void AddExtraFeeLines(ExtraFeeLine extraFeeLine, DateOnly startDate, DateOnly endDate, int requestedYear, int startDateMonth, bool isProratedMonth, int days, List<LedgerLine> lines, ref int lineNumber)
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
    private static int CalculateNumberOfDays(DateOnly startDate, DateOnly endDate, BillingType billingType, bool isDepartureMonthYear, bool isLastDayOfMonth)
    {
        if (endDate < startDate) return 0;
        if (endDate == startDate) return billingType == BillingType.Nightly && isDepartureMonthYear ? 0 : 1;

        var days = endDate.DayNumber - startDate.DayNumber;
        if (billingType != BillingType.Nightly ||
           (billingType == BillingType.Nightly && !isDepartureMonthYear && isLastDayOfMonth))
            days++;
        return days;
    }
    #endregion

    #region PreBilling
    public async Task<IReadOnlyList<Invoice>> GetPreBillingInvoicesAsync(Guid organizationId, string officeIds, DateOnly billingMonth)
        => await GetUnbilledInvoicePreviewsAsync(organizationId, officeIds, billingMonth);

    private async Task<IReadOnlyList<Invoice>> GetUnbilledInvoicePreviewsAsync(Guid organizationId, string officeIds, DateOnly billingMonth)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return Array.Empty<Invoice>();

        if (billingMonth == default || billingMonth.Day != 1)
            throw new ArgumentException("BillingMonth must be the first day of the month.", nameof(billingMonth));

        var monthStart = billingMonth;
        var monthEnd = LastDayOfMonth(billingMonth);

        // Get active reservations for the selected offices that overlap the billing month.
        var activeReservations = (await _reservationRepository.GetActiveReservationsByOfficeIdsAsync(organizationId, officeIds))
            .Where(reservation => ReservationOverlapsBillingMonth(
                ResolveBillingArrivalDate(reservation),
                ResolveBillingDepartureDate(reservation),
                monthStart,
                monthEnd))
            .OrderBy(reservation => reservation.OfficeName)
            .ThenBy(reservation => reservation.ReservationCode)
            .ToList();

        if (activeReservations.Count == 0)
            return Array.Empty<Invoice>();

        // Get active invoices for the selected offices and billing month (headers only; no ledger lines).
        var billedReservationIds = (await _accountingRepository.GetActiveInvoicesByAccountingMonthAsync(new ActiveInvoiceByAccountingMonthCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            AccountingPeriod = billingMonth
        }))
            .Where(invoice => invoice.ReservationId.HasValue && invoice.ReservationId.Value != Guid.Empty)
            .Select(invoice => invoice.ReservationId!.Value)
            .ToHashSet();

        // Drop stays that already have an invoice for this accounting period.
        var unbilledReservations = activeReservations
            .Where(reservation => !billedReservationIds.Contains(reservation.ReservationId))
            .ToList();

        if (unbilledReservations.Count == 0)
            return Array.Empty<Invoice>();

        // Build preview invoices for the remaining stays.
        var previewInvoices = new List<Invoice>();
        var accountingStartsByOffice = new Dictionary<int, DateOnly>();

        foreach (var reservation in unbilledReservations)
        {
            if (!accountingStartsByOffice.TryGetValue(reservation.OfficeId, out var accountingStart))
            {
                var resolvedStart = await TryGetAccountingStartDateAsync(organizationId, reservation.OfficeId);
                if (!resolvedStart.HasValue)
                    continue;

                accountingStart = resolvedStart.Value;
                accountingStartsByOffice[reservation.OfficeId] = accountingStart;
            }

            if (billingMonth < accountingStart)
                continue;

            var (periodStart, periodEnd) = ResolveBillingPeriodForMonth(reservation, billingMonth);
            if (!HasBillablePreviewPeriod(reservation, periodStart, periodEnd))
                continue;

            var ledgerLines = await CreateLedgerLinesForReservationIdAsync(
                reservation,
                invoiceDate: monthStart,
                startDate: periodStart,
                endDate: periodEnd);

            if (ledgerLines.Count == 0)
                continue;

            var totalAmount = ledgerLines.Sum(line => line.Amount);
            var responsibleParty = await ResolvePreBillingResponsiblePartyAsync(organizationId, reservation);

            previewInvoices.Add(BuildPreBillingInvoicePreview(
                organizationId,
                reservation,
                billingMonth,
                periodStart,
                periodEnd,
                ledgerLines,
                totalAmount,
                responsibleParty: responsibleParty));
        }

        return previewInvoices;
    }
    #endregion

    #region MissingInvoice
    public async Task<IReadOnlyList<Invoice>> GetMissingInvoicesAsync(Guid organizationId, string officeIds)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return Array.Empty<Invoice>();

        var throughMonth = FirstDayOfMonth(DateOnly.FromDateTime(DateTime.Today));

        var activeReservations = (await _reservationRepository.GetActiveReservationsByOfficeIdsAsync(organizationId, officeIds))
            .OrderBy(reservation => reservation.OfficeName)
            .ThenBy(reservation => reservation.ReservationCode)
            .ToList();

        if (activeReservations.Count == 0)
            return Array.Empty<Invoice>();

        var existingInvoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludePaid = true
        }))
            .Where(invoice => invoice.ReservationId.HasValue && invoice.ReservationId.Value != Guid.Empty && invoice.AccountingPeriod != default)
            .ToList();

        var existingInvoicePeriods = existingInvoices
            .Select(invoice => (ReservationId: invoice.ReservationId!.Value, Period: FirstDayOfMonth(invoice.AccountingPeriod)))
            .ToHashSet();

        var previewInvoices = new List<Invoice>();

        foreach (var reservation in activeReservations)
        {
            var reservationInvoices = existingInvoices
                .Where(invoice => invoice.ReservationId == reservation.ReservationId)
                .ToList();

            await AddUnbilledPreviewsForReservationAsync(
                organizationId,
                reservation,
                throughMonth,
                existingInvoicePeriods,
                reservationInvoices,
                previewInvoices);
        }

        return previewInvoices
            .OrderBy(invoice => invoice.OfficeName)
            .ThenBy(invoice => invoice.ReservationCode)
            .ThenBy(invoice => invoice.AccountingPeriod)
            .ToList();
    }
    #endregion

    #region ReservationInvoicePreview
    public async Task<IReadOnlyList<Invoice>> GetReservationInvoicePreviewsAsync(Guid organizationId, Guid reservationId)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId);
        if (reservation == null)
            return Array.Empty<Invoice>();

        var existingInvoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(),
            ReservationId = reservationId,
            IsActive = true,
            IncludePaid = true
        }))
            .Where(invoice => invoice.ReservationId.HasValue && invoice.ReservationId.Value != Guid.Empty && invoice.AccountingPeriod != default)
            .ToList();

        var existingInvoicePeriods = existingInvoices
            .Select(invoice => (ReservationId: invoice.ReservationId!.Value, Period: FirstDayOfMonth(invoice.AccountingPeriod)))
            .ToHashSet();

        var previewInvoices = new List<Invoice>();
        await AddUnbilledPreviewInvoicesThroughEndOfStayAsync(
            organizationId,
            reservation,
            existingInvoicePeriods,
            existingInvoices,
            previewInvoices);

        return previewInvoices
            .OrderBy(invoice => invoice.AccountingPeriod)
            .ToList();
    }

    private async Task AddUnbilledPreviewInvoicesThroughEndOfStayAsync(Guid organizationId, Reservation reservation, HashSet<(Guid ReservationId, DateOnly Period)> existingInvoicePeriods, IReadOnlyList<Invoice> existingReservationInvoices, List<Invoice> previewInvoices)
    {
        var throughMonth = FirstDayOfMonth(ResolveBillingDepartureDate(reservation));
        await AddUnbilledPreviewsForReservationAsync(
            organizationId,
            reservation,
            throughMonth,
            existingInvoicePeriods,
            existingReservationInvoices,
            previewInvoices);
    }

    private async Task AddUnbilledPreviewsForReservationAsync(Guid organizationId, Reservation reservation, DateOnly throughMonth, HashSet<(Guid ReservationId, DateOnly Period)> existingInvoicePeriods, IReadOnlyList<Invoice> existingReservationInvoices, List<Invoice> previewInvoices)
    {
        var accountingStart = await TryGetAccountingStartDateAsync(organizationId, reservation.OfficeId);
        if (!accountingStart.HasValue)
            return;

        var nextSequence = ResolveNextInvoiceSequence(reservation, existingReservationInvoices);
        var responsibleParty = await ResolvePreBillingResponsiblePartyAsync(organizationId, reservation);

        foreach (var billingMonth in EnumerateBillableMonths(reservation, throughMonth, accountingStart.Value))
        {
            if (existingInvoicePeriods.Contains((reservation.ReservationId, billingMonth)))
                continue;

            var (periodStart, periodEnd) = ResolveBillingPeriodForMonth(reservation, billingMonth);

            var monthStart = billingMonth;
            var ledgerLines = await CreateLedgerLinesForReservationIdAsync(
                reservation,
                invoiceDate: monthStart,
                startDate: periodStart,
                endDate: periodEnd);

            if (ledgerLines.Count == 0)
                continue;

            var totalAmount = ledgerLines.Sum(line => line.Amount);

            previewInvoices.Add(BuildPreBillingInvoicePreview(
                organizationId,
                reservation,
                billingMonth,
                periodStart,
                periodEnd,
                ledgerLines,
                totalAmount,
                nextSequence,
                responsibleParty));
            nextSequence++;
        }
    }

    private static DateOnly ResolveLastBillableMonth(Reservation reservation)
    {
        var departureDate = ResolveBillingDepartureDate(reservation);
        var arrivalDate = ResolveBillingArrivalDate(reservation);

        if (reservation.BillingType == BillingType.Nightly)
        {
            var lastNight = departureDate.AddDays(-1);
            if (lastNight < arrivalDate)
                return FirstDayOfMonth(departureDate);

            return FirstDayOfMonth(lastNight);
        }

        return FirstDayOfMonth(departureDate);
    }

    private static (DateOnly PeriodStart, DateOnly PeriodEnd) ResolveBillingPeriodForMonth(Reservation reservation, DateOnly billingMonth)
    {
        var monthStart = billingMonth;
        var monthEnd = LastDayOfMonth(billingMonth);
        var arrivalDate = ResolveBillingArrivalDate(reservation);
        var departureDate = ResolveBillingDepartureDate(reservation);
        var periodStart = arrivalDate > monthStart ? arrivalDate : monthStart;
        var periodEnd = departureDate < monthEnd ? departureDate : monthEnd;
        return (periodStart, periodEnd);
    }

    private static bool HasBillablePreviewPeriod(Reservation reservation, DateOnly periodStart, DateOnly periodEnd)
    {
        if (periodEnd < periodStart)
            return false;

        if (reservation.BillingType == BillingType.Nightly
            && periodStart == periodEnd
            && periodStart == ResolveBillingDepartureDate(reservation))
            return false;

        return true;
    }

    private static IEnumerable<DateOnly> EnumerateBillableMonths(Reservation reservation, DateOnly throughMonth, DateOnly accountingStartMonth)
    {
        var arrivalDate = ResolveBillingArrivalDate(reservation);
        var departureDate = ResolveBillingDepartureDate(reservation);
        var startMonth = FirstDayOfMonth(arrivalDate);
        if (startMonth < accountingStartMonth)
            startMonth = accountingStartMonth;

        var departureMonth = FirstDayOfMonth(departureDate);
        var scanThrough = throughMonth <= departureMonth ? throughMonth : departureMonth;

        if (startMonth > scanThrough)
            yield break;

        for (var month = startMonth; month <= scanThrough; month = month.AddMonths(1))
        {
            if (!ReservationOverlapsBillingMonth(arrivalDate, departureDate, month, LastDayOfMonth(month)))
                continue;

            var (periodStart, periodEnd) = ResolveBillingPeriodForMonth(reservation, month);
            if (!HasBillablePreviewPeriod(reservation, periodStart, periodEnd))
                continue;

            yield return month;
        }
    }

    private async Task<DateOnly?> TryGetAccountingStartDateAsync(Guid organizationId, int officeId)
    {
        var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
        return accountingOffice == null
            ? null
            : AccountingOfficePeriodBoundary.GetStartMonth(accountingOffice);
    }

    private static int ResolveNextInvoiceSequence(Reservation reservation, IReadOnlyList<Invoice> existingReservationInvoices)
    {
        var maxSequence = reservation.CurrentInvoiceNo;
        var prefix = $"{reservation.ReservationCode}-";

        foreach (var invoice in existingReservationInvoices)
        {
            var code = invoice.InvoiceCode?.Trim();
            if (string.IsNullOrWhiteSpace(code)
                || !code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || code.Length <= prefix.Length)
            {
                continue;
            }

            if (int.TryParse(code.AsSpan(prefix.Length), out var sequence))
                maxSequence = Math.Max(maxSequence, sequence);
        }

        return maxSequence + 1;
    }

    #endregion

    #region Billing Preview Helpers
    private static bool ReservationOverlapsBillingMonth(DateOnly arrivalDate, DateOnly departureDate, DateOnly monthStart, DateOnly monthEnd)
        => arrivalDate <= monthEnd && departureDate >= monthStart;

    private static Invoice BuildPreBillingInvoicePreview(Guid organizationId, Reservation reservation, DateOnly billingMonth, DateOnly monthStart, DateOnly monthEnd, List<LedgerLine> ledgerLines, decimal totalAmount, int? invoiceSequence = null, string? responsibleParty = null)
    {
        var sequenceNumber = invoiceSequence ?? reservation.CurrentInvoiceNo + 1;
        var invoiceCode = $"{reservation.ReservationCode}-{sequenceNumber:000}";

        return new Invoice
        {
            InvoiceId = Guid.Empty,
            OrganizationId = organizationId,
            OfficeId = reservation.OfficeId,
            OfficeName = reservation.OfficeName,
            InvoiceCode = invoiceCode,
            ReservationId = reservation.ReservationId,
            ReservationCode = reservation.ReservationCode,
            PropertyId = reservation.PropertyId,
            PropertyCode = NormalizeOptionalString(reservation.PropertyCode),
            ContactId = ResolveInvoiceResponsibleContactId(reservation),
            ContactName = responsibleParty,
            CompanyId = reservation.CompanyId,
            CompanyName = reservation.CompanyName,
            ResponsibleParty = responsibleParty,
            InvoiceDate = monthStart,
            DueDate = monthStart,
            AccountingPeriod = billingMonth,
            InvoicePeriod = FormatInvoicePeriod(monthStart, monthEnd),
            TotalAmount = totalAmount,
            PaidAmount = 0,
            IsActive = true,
            LedgerLines = ledgerLines
        };
    }

    private async Task<string?> ResolvePreBillingResponsiblePartyAsync(Guid organizationId, Reservation reservation)
    {
        if (reservation.ReservationType is ReservationType.Corporate or ReservationType.Platform)
        {
            if (NormalizeOptionalGuid(reservation.CompanyId) is { } companyId)
            {
                var companyContact = await _contactRepository.GetContactByIdsAsync(companyId, organizationId);
                var companyName = NormalizeOptionalString(companyContact?.CompanyName);
                if (companyName != null)
                    return companyName;
            }

            return NormalizeOptionalString(reservation.CompanyName) ?? NormalizeOptionalString(reservation.ContactName);
        }

        return NormalizeOptionalString(reservation.ContactName);
    }
    #endregion

    #region Payment Document
    private async Task EnsurePaymentCodeAsync(Payment payment)
    {
        if (!string.IsNullOrWhiteSpace(payment.PaymentCode))
            return;

        var paymentCode = await _organizationManager.GenerateEntityCodeAsync(payment.OrganizationId, EntityType.Payment);
        if (string.IsNullOrWhiteSpace(paymentCode))
            throw new Exception("Unable to generate payment code");

        payment.PaymentCode = paymentCode.Trim();
    }

    public async Task<Payment> ApplyInvoicePaymentAsync(Payment payment, IReadOnlyList<Guid>? autoSplitInvoiceIds, IReadOnlyList<PaymentInvoiceAllocation>? explicitAllocations, string officeAccess, Guid currentUser)
    {
        EnsureInvoicePayment(payment);

        if (explicitAllocations != null && explicitAllocations.Count > 0)
            return await ApplyInvoicePaymentWithExplicitAllocationsAsync(payment, explicitAllocations, officeAccess, currentUser);

        if (autoSplitInvoiceIds != null && autoSplitInvoiceIds.Count > 0)
            return await ApplyInvoicePaymentWithAutoSplitAsync(payment, autoSplitInvoiceIds, officeAccess, currentUser);

        throw new ArgumentException("At least one invoice or allocation is required.", nameof(autoSplitInvoiceIds));
    }

    private async Task<Payment> ApplyInvoicePaymentWithAutoSplitAsync(Payment payment, IReadOnlyList<Guid> invoiceIds, string officeAccess, Guid currentUser)
    {
        await EnsurePaymentCodeAsync(payment);
        var createdPayment = await _accountingRepository.CreatePaymentAsync(payment);

        var invoicePayment = await ApplyPaymentToInvoicesAsync(invoiceIds.ToList(), payment.OrganizationId, officeAccess, payment.CostCodeId, payment.Description, payment.Amount, payment.PaymentDate, currentUser);

        await LinkInvoicePaymentApplicationsAsync(createdPayment.PaymentId, invoicePayment, currentUser);
        await CreateJournalEntriesFromInvoicePaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    private async Task<Payment> ApplyInvoicePaymentWithExplicitAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser)
    {
        ValidateExplicitPaymentAllocations(payment, allocations);

        Payment? createdPayment = null;
        try
        {
            await EnsurePaymentCodeAsync(payment);
            createdPayment = await _accountingRepository.CreatePaymentWithInvoiceAllocationsAsync(payment, allocations, currentUser);
            await CreateJournalEntriesFromInvoicePaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);
        }
        catch
        {
            if (createdPayment != null)
                await TryDeleteIncompletePaymentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

            throw;
        }

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    private async Task<Payment> UpdateInvoicePaymentWithExplicitAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required.", nameof(payment));

        ValidateExplicitPaymentAllocations(payment, allocations);

        var existing = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, payment.OrganizationId);
        if (existing == null)
            throw new Exception("Payment record not found");

        if (existing.PaymentDirectionId != (int)PaymentDirection.Inbound)
            throw new Exception("Bill payments must be updated through bill allocations.");

        payment.PaymentCode = existing.PaymentCode;
        payment.DepositId = existing.DepositId;

        await ClearPaymentDocumentLinksAsync(existing.OrganizationId, existing.PaymentId, currentUser);
        await DeleteJournalEntriesForPaymentAsync(existing);

        var updatedPayment = await _accountingRepository.UpdatePaymentWithInvoiceAllocationsAsync(payment, allocations, currentUser);
        await CreateJournalEntriesFromInvoicePaymentDocumentAsync(updatedPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(updatedPayment.PaymentId, payment.OrganizationId)
            ?? updatedPayment;
    }

    private static void ValidateExplicitPaymentAllocations(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations)
    {
        EnsureInvoicePayment(payment);

        if (allocations == null || allocations.Count == 0)
            throw new ArgumentException("At least one invoice allocation is required.", nameof(allocations));

        var allocationTotal = allocations.Sum(allocation => allocation.Amount);
        if (allocationTotal != payment.Amount)
            throw new ArgumentException("Allocation total must equal the payment amount.", nameof(allocations));
    }

    private async Task TryDeleteIncompletePaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        try
        {
            var existing = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (existing != null)
                await DeletePaymentAsync(paymentId, organizationId, currentUser);
        }
        catch
        {
            // Best-effort cleanup after a failed create.
        }
    }

    private static LedgerLine ToInvoicePaymentLedgerLine(PaymentLedgerLine paymentLine)
        => new()
        {
            LedgerLineId = paymentLine.LedgerLineId,
            InvoiceId = paymentLine.InvoiceId,
            LineNumber = paymentLine.LineNumber,
            ReservationId = paymentLine.ReservationId,
            CostCodeId = paymentLine.CostCodeId,
            Amount = paymentLine.Amount,
            Description = paymentLine.Description,
            LedgerLineDate = paymentLine.LedgerLineDate,
            PaymentId = paymentLine.PaymentId
        };

    private async Task LinkInvoicePaymentApplicationsAsync(Guid paymentId, InvoicePayment invoicePayment, Guid currentUser)
    {
        foreach (var application in invoicePayment.PaymentApplications)
        {
            await _accountingRepository.SetLedgerLinePaymentIdAsync(application.PaymentLedgerLine.LedgerLineId, paymentId, currentUser);
            application.PaymentLedgerLine.PaymentId = paymentId;
        }
    }

    private async Task SyncLinkedPaymentAmountsFromInvoiceAsync(Invoice invoice, Guid currentUser)
    {
        var paymentIds = invoice.LedgerLines
            .Where(line => line.PaymentId is { } paymentId && paymentId != Guid.Empty)
            .Select(line => line.PaymentId!.Value)
            .Distinct()
            .ToList();

        foreach (var paymentId in paymentIds)
            await SyncPaymentAmountFromLinkedLedgerLinesAsync(paymentId, invoice.OrganizationId, currentUser);
    }

    private async Task SyncPaymentAmountFromLinkedLedgerLinesAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        if (paymentId == Guid.Empty)
            return;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
            return;

        var linkedLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(paymentId, organizationId);
        if (linkedLines.Count == 0)
            return;

        var linkedTotal = linkedLines.Sum(line => line.Amount);
        if (payment.Amount == linkedTotal)
            return;

        payment.Amount = linkedTotal;
        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
    }

    private static void EnsureInvoicePayment(Payment payment)
    {
        if (payment.PaymentDirectionId != (int)PaymentDirection.Inbound)
            throw new ArgumentException("Invoice allocations are only supported for invoice payments.", nameof(payment));
    }
    #endregion
}

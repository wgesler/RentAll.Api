using RentAll.Domain.Constants;
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

        await TryAddCompanyMarkupOrReferralLedgerLineAsync(reservation, ledgerLines, costCodeById);
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

    private static int ResolveDefaultCostCodeId(IReadOnlyDictionary<int, CostCode> costCodeById, int configuredCostCodeId, string descriptionContains, string? descriptionExcludes = null)
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

    private static bool IsRentalFeeLedgerLine(LedgerLine line)
        => line.Description.StartsWith("Rental Fee", StringComparison.Ordinal);

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
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, arrivalDate, departureDate, startDateYear, startDateMonth, isProratedMonth, days, lineItems, ref lineNumber);
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
                AddExtraFeeLines(extraFeeLine, firstDay, lastDay, startDateYear, startDateMonth, isProratedMonth, days, lineItems, ref lineNumber);
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
        if (extraFeeLine.FeeFrequency == FrequencyType.OneTime)
            return;

        if (startDate > endDate)
            return;

        var fees = CountScheduledOccurrences(startDate, startDate, endDate, extraFeeLine.FeeFrequency);
        if (fees <= 0)
            return;

        var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
        decimal amount;
        if (extraFeeLine.FeeFrequency == FrequencyType.Monthly && days > 0 && days < daysInMonth)
            amount = (extraFeeLine.FeeAmount / PRORATE_DAYS) * days;
        else
            amount = fees * extraFeeLine.FeeAmount;

        lines.Add(new LedgerLine
        {
            LineNumber = lineNumber++,
            Description = $"{extraFeeLine.FeeDescription}",
            Amount = amount,
            CostCodeId = extraFeeLine.CostCodeId
        });
    }

    #endregion

    #region Day Calculation Methods
    private static int CalculateNumberOfDays(DateOnly startDate, DateOnly endDate, BillingType billingType, bool isDepartureMonthYear, bool isLastDayOfMonth)
        => InvoiceBillingDays.CalculateNumberOfDays(startDate, endDate, billingType, isDepartureMonthYear, isLastDayOfMonth);
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

            await CreateDefaultCostCodeAsync(reservation.OrganizationId, reservation.OfficeId);
            var costCodeById = await LoadCostCodeByOfficeIdAsync(reservation.OrganizationId, reservation.OfficeId);
            ApplyMissingDefaultCostCodesFromOfficeCostCodes(costCodeById);

            var totalAmount = ledgerLines.Sum(line => line.Amount);
            var responsibleParty = await ResolvePreBillingResponsiblePartyAsync(organizationId, reservation);

            var nextSequence = ResolveNextInvoiceSequence(reservation, Array.Empty<Invoice>());
            var mainPreview = BuildPreBillingInvoicePreview(
                organizationId,
                reservation,
                billingMonth,
                periodStart,
                periodEnd,
                ledgerLines,
                totalAmount,
                nextSequence,
                responsibleParty: responsibleParty);
            previewInvoices.Add(mainPreview);
            nextSequence++;
            TryAppendReferralSeparateInvoicePreview(
                reservation,
                mainPreview,
                previewInvoices,
                ref nextSequence,
                costCodeById);
        }

        return previewInvoices;
    }
    #endregion

    #region MissingInvoice
    public async Task<IReadOnlyList<Invoice>> GetMissingInvoicesAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return Array.Empty<Invoice>();

        var context = await LoadBilledMatchupOfficeContextAsync(organizationId, officeIds);
        await RebuildBilledRowsAsync(organizationId, context, context.ActiveReservations, currentUser);
        await _accountingRepository.DeleteBilledByOrganizationAndOfficeIdsExceptReservationsAsync(
            organizationId,
            officeIds,
            context.ActiveReservations.Select(reservation => reservation.ReservationId).ToList());

        var currentMonth = FirstDayOfMonth(DateOnly.FromDateTime(DateTime.Today));

        var activeReservations = context.ActiveReservations
            .OrderBy(reservation => reservation.OfficeName)
            .ThenBy(reservation => reservation.ReservationCode)
            .ToList();

        if (activeReservations.Count == 0)
            return Array.Empty<Invoice>();

        var activeReservationIds = activeReservations
            .Select(reservation => reservation.ReservationId)
            .ToHashSet();

        var billedReportRows = (await _accountingRepository.GetBilledByOrganizationAndOfficeIdsAsync(organizationId, officeIds))
            .Where(row => row.MonthStart <= currentMonth)
            .Where(row => activeReservationIds.Contains(row.ReservationId))
            .Where(row => row.DaysStayed > row.DaysBilled && !row.Ignore)
            .OrderBy(row => row.ReservationCode)
            .ThenBy(row => row.MonthStart)
            .ToList();

        if (billedReportRows.Count == 0)
            return Array.Empty<Invoice>();

        var reservationsById = activeReservations
            .GroupBy(reservation => reservation.ReservationId)
            .ToDictionary(group => group.Key, group => group.First());
        var previewInvoices = new List<Invoice>();

        foreach (var billedRowsByReservation in billedReportRows.GroupBy(row => row.ReservationId))
        {
            if (!reservationsById.TryGetValue(billedRowsByReservation.Key, out var reservation))
                continue;

            if (!context.AccountingOfficesByOfficeId.TryGetValue(reservation.OfficeId, out var accountingOffice))
                continue;

            var invoiceStart = AccountingOfficePeriodBoundary.GetInvoiceStart(accountingOffice);
            context.InvoicesByReservationId.TryGetValue(reservation.ReservationId, out var reservationInvoices);
            reservationInvoices ??= [];

            try
            {
                await AppendInvoicePreviewsForOpenBilledRowsAsync(
                    organizationId,
                    reservation,
                    billedRowsByReservation.OrderBy(row => row.MonthStart).ToList(),
                    reservationInvoices,
                    invoiceStart,
                    previewInvoices);
            }
            catch (Exception ex)
            {
                LogApplicationDiagnostic(
                    "GetMissingInvoicesPreviewReservation",
                    $"ReservationCode={reservation.ReservationCode}",
                    ex,
                    organizationId,
                    reservation.OfficeId);
            }
        }

        return previewInvoices
            .OrderBy(invoice => invoice.OfficeName)
            .ThenBy(invoice => invoice.ReservationCode)
            .ThenBy(invoice => invoice.AccountingPeriod)
            .ToList();
    }
    #endregion

    #region ReservationInvoicePreview
    public async Task<IReadOnlyList<Invoice>> GetReservationInvoicePreviewsAsync(Guid organizationId, Guid reservationId, Guid currentUser)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId);
        if (reservation == null)
            return Array.Empty<Invoice>();

        await RebuildReservationBilledMatchupForReservationAsync(organizationId, reservation, currentUser);

        var currentMonth = FirstDayOfMonth(DateOnly.FromDateTime(DateTime.Today));

        var accountingOfficesByOfficeId = (await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(
                organizationId,
                reservation.OfficeId.ToString()))
            .ToDictionary(office => office.OfficeId);

        if (!accountingOfficesByOfficeId.TryGetValue(reservation.OfficeId, out var accountingOffice))
            return Array.Empty<Invoice>();

        var invoiceStart = AccountingOfficePeriodBoundary.GetInvoiceStart(accountingOffice);

        var existingInvoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(),
            ReservationId = reservationId,
            IsActive = true,
            IncludePaid = true
        }))
            .Where(invoice => invoice.ReservationId.HasValue && invoice.ReservationId.Value != Guid.Empty && invoice.AccountingPeriod != default)
            .Where(invoice => RentalFeeLineParser.IsAccountingPeriodOnOrAfterInvoiceStart(invoice, invoiceStart))
            .ToList();

        var billedRows = (await _accountingRepository.GetBilledByReservationIdAsync(organizationId, reservationId)).ToList();
        var latestInvoicedMonth = billedRows
            .Where(row => row.DaysBilled > 0)
            .Select(row => (DateOnly?)row.MonthStart)
            .Max();
        var openBilledRows = billedRows
            .Where(row => row.DaysStayed > row.DaysBilled && !row.Ignore)
            .Where(row => IncludeOpenMonthInReservationPreview(row, currentMonth, latestInvoicedMonth))
            .OrderBy(row => row.MonthStart)
            .ToList();

        if (openBilledRows.Count == 0)
            return Array.Empty<Invoice>();

        var previewInvoices = new List<Invoice>();
        try
        {
            await AppendInvoicePreviewsForOpenBilledRowsAsync(
                organizationId,
                reservation,
                openBilledRows,
                existingInvoices,
                invoiceStart,
                previewInvoices);
        }
        catch (Exception ex)
        {
            LogApplicationDiagnostic(
                "GetReservationInvoicePreviewsAsync",
                $"ReservationCode={reservation.ReservationCode}",
                ex,
                organizationId,
                reservation.OfficeId);
        }

        return previewInvoices
            .OrderBy(invoice => invoice.AccountingPeriod)
            .ToList();
    }

    /// <summary>
    /// Current and future months always preview. An earlier month previews when it is already partly invoiced,
    /// or when a later month is invoiced and this one is still open.
    /// </summary>
    private static bool IncludeOpenMonthInReservationPreview(Billed row, DateOnly currentMonth, DateOnly? latestInvoicedMonth)
    {
        if (row.MonthStart >= currentMonth)
            return true;
        if (row.DaysBilled > 0)
            return true;
        return latestInvoicedMonth.HasValue && row.MonthStart < latestInvoicedMonth.Value;
    }

    private async Task AppendInvoicePreviewsForOpenBilledRowsAsync(
        Guid organizationId,
        Reservation reservation,
        IReadOnlyList<Billed> openBilledRows,
        IReadOnlyList<Invoice> reservationInvoices,
        DateOnly invoiceStart,
        List<Invoice> previewInvoices)
    {
        if (openBilledRows.Count == 0)
            return;

        var nextSequence = ResolveNextInvoiceSequence(reservation, reservationInvoices);
        var responsibleParty = await ResolvePreBillingResponsiblePartyAsync(organizationId, reservation);

        await CreateDefaultCostCodeAsync(reservation.OrganizationId, reservation.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(reservation.OrganizationId, reservation.OfficeId);
        ApplyMissingDefaultCostCodesFromOfficeCostCodes(costCodeById);
        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, reservation.OrganizationId);
        var isFurnished = property == null || !property.Unfurnished;
        var rentalCostCodeId = isFurnished ? FURNISHED_EXPENSE_COST_CODE : UNFURNISHED_EXPENSE_COST_CODE;

        foreach (var billedRow in openBilledRows)
        {
            try
            {
                nextSequence = await AddMissingInvoicePreviewsForBilledMonthAsync(
                    organizationId,
                    reservation,
                    billedRow,
                    reservationInvoices,
                    invoiceStart,
                    previewInvoices,
                    nextSequence,
                    responsibleParty,
                    costCodeById,
                    rentalCostCodeId);
            }
            catch (Exception ex)
            {
                LogApplicationDiagnostic(
                    "GetMissingInvoicesPreviewRow",
                    $"ReservationCode={reservation.ReservationCode} MonthStart={billedRow.MonthStart:yyyy-MM-dd} BilledId={billedRow.BilledId}",
                    ex,
                    organizationId,
                    reservation.OfficeId);
            }
        }
    }

    private async Task<int> AddMissingInvoicePreviewsForBilledMonthAsync(
        Guid organizationId,
        Reservation reservation,
        Billed billedRow,
        IReadOnlyList<Invoice> reservationInvoices,
        DateOnly invoiceStart,
        List<Invoice> previewInvoices,
        int nextSequence,
        string? responsibleParty,
        IReadOnlyDictionary<int, CostCode> costCodeById,
        int rentalCostCodeId)
    {
        var billingMonth = billedRow.MonthStart;
        var periodStart = billedRow.PeriodStart;
        var periodEnd = billedRow.PeriodEnd;
        var gaps = RentalFeeLineParser.FindUncoveredBillingRangesForPeriod(
            periodStart,
            periodEnd,
            reservationInvoices,
            invoiceStart).ToList();

        if (gaps.Count == 0)
            return nextSequence;

        var ledgerLines = new List<LedgerLine>();
        var rentalDaysAlreadyBilled = SumRentalDaysBilledInMonth(reservationInvoices, reservation, billingMonth);
        foreach (var (gapStart, gapEnd) in gaps)
        {
            var (gapLines, rentalDaysCharged) = await CreateLedgerLinesForUncoveredStayAsync(
                reservation,
                billingMonth,
                periodStart,
                periodEnd,
                gapStart,
                gapEnd,
                costCodeById,
                rentalCostCodeId,
                rentalDaysAlreadyBilled);
            rentalDaysAlreadyBilled += rentalDaysCharged;
            ledgerLines.AddRange(gapLines);
        }

        if (ledgerLines.Count == 0)
            return nextSequence;

        for (var lineIndex = 0; lineIndex < ledgerLines.Count; lineIndex++)
            ledgerLines[lineIndex].LineNumber = lineIndex + 1;

        var previewStart = gaps.Min(gap => gap.Start);
        var previewEnd = gaps.Max(gap => gap.End);
        var totalAmount = ledgerLines.Sum(line => line.Amount);
        var preview = BuildPreBillingInvoicePreview(
            organizationId,
            reservation,
            billingMonth,
            previewStart,
            previewEnd,
            ledgerLines,
            totalAmount,
            nextSequence,
            responsibleParty);

        preview.BilledId = billedRow.BilledId;
        preview.BilledIgnore = billedRow.Ignore;
        preview.BilledRentalFeeLines = billedRow.RentalFeeLines is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(billedRow.RentalFeeLines)
            : null;
        ApplyMissingPreviewBilledGridMetrics(preview, billedRow);

        previewInvoices.Add(preview);
        nextSequence++;
        TryAppendReferralSeparateInvoicePreview(
            reservation,
            preview,
            previewInvoices,
            ref nextSequence,
            costCodeById);

        return nextSequence;
    }

    private async Task<(List<LedgerLine> Lines, int RentalDaysCharged)> CreateLedgerLinesForUncoveredStayAsync(
        Reservation reservation,
        DateOnly billingMonth,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly gapStart,
        DateOnly gapEnd,
        IReadOnlyDictionary<int, CostCode>? costCodeById = null,
        int? rentalCostCodeId = null,
        int rentalDaysAlreadyBilledInMonth = 0)
    {
        if (gapStart == periodStart && gapEnd == periodEnd)
        {
            var fullPeriodLines = await CreateLedgerLinesForReservationIdAsync(
                reservation,
                invoiceDate: billingMonth,
                startDate: periodStart,
                endDate: periodEnd);
            return (fullPeriodLines, CountRentalDays(fullPeriodLines, reservation, billingMonth));
        }

        return await CreateLedgerLinesForBillingGapAsync(
            reservation,
            billingMonth,
            gapStart,
            gapEnd,
            costCodeById,
            rentalCostCodeId,
            rentalDaysAlreadyBilledInMonth);
    }

    private async Task<(List<LedgerLine> Lines, int RentalDaysCharged)> CreateLedgerLinesForBillingGapAsync(
        Reservation reservation,
        DateOnly invoiceDate,
        DateOnly gapStart,
        DateOnly gapEnd,
        IReadOnlyDictionary<int, CostCode>? costCodeById = null,
        int? rentalCostCodeId = null,
        int rentalDaysAlreadyBilledInMonth = 0)
    {
        if (costCodeById == null)
        {
            await CreateDefaultCostCodeAsync(reservation.OrganizationId, reservation.OfficeId);
            costCodeById = await LoadCostCodeByOfficeIdAsync(reservation.OrganizationId, reservation.OfficeId);
            ApplyMissingDefaultCostCodesFromOfficeCostCodes(costCodeById);
        }

        if (!rentalCostCodeId.HasValue)
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, reservation.OrganizationId);
            var isFurnished = property == null || !property.Unfurnished;
            rentalCostCodeId = isFurnished ? FURNISHED_EXPENSE_COST_CODE : UNFURNISHED_EXPENSE_COST_CODE;
        }

        var lineItems = new List<LedgerLine>();
        var lineNumber = 1;
        var departureDate = ResolveBillingDepartureDate(reservation);
        var lastDayOfMonth = new DateOnly(
            gapStart.Year,
            gapStart.Month,
            DateTime.DaysInMonth(gapStart.Year, gapStart.Month));
        var isDepartureMonthYear = gapEnd.Month == departureDate.Month && gapEnd.Year == departureDate.Year;
        var isLastDayOfMonth = gapEnd.Day == lastDayOfMonth.Day;
        var daysInMonth = DateTime.DaysInMonth(gapStart.Year, gapStart.Month);
        var days = CalculateNumberOfDays(
            gapStart,
            gapEnd,
            reservation.BillingType,
            isDepartureMonthYear,
            isLastDayOfMonth);
        var rentalDaysCharged = days;
        if (reservation.BillingType == BillingType.Monthly)
        {
            days = ResolveMonthlyExtensionDays(rentalDaysAlreadyBilledInMonth, days, daysInMonth);
            rentalDaysCharged = days;
        }

        AddRentalLine(
            days,
            reservation,
            gapStart,
            gapEnd,
            daysInMonth,
            isDepartureMonthYear,
            isLastDayOfMonth,
            lineItems,
            ref lineNumber,
            rentalCostCodeId!.Value);
        AddMaidServiceLines(reservation, gapStart, gapEnd, gapStart.Year, gapStart.Month, lineItems, ref lineNumber);
        var arrivalDate = ResolveBillingArrivalDate(reservation);
        var isArrivalMonthExtension = gapStart > arrivalDate
            && gapStart.Year == arrivalDate.Year
            && gapStart.Month == arrivalDate.Month;
        if (!isArrivalMonthExtension)
        {
            var isFirstMonth = gapStart.Month == arrivalDate.Month && gapStart.Year == arrivalDate.Year;
            GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
            AddDepartureFeeIfApplicable(reservation, invoiceDate, lineItems, ref lineNumber);
        }
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
            AddExtraFeeLines(extraFeeLine, gapStart, gapEnd, gapStart.Year, gapStart.Month, isProratedMonth: days < daysInMonth, days, lineItems, ref lineNumber);

        foreach (var ledgerLine in lineItems)
        {
            ledgerLine.LedgerLineDate = invoiceDate;
            ledgerLine.ReservationId = reservation.ReservationId;
            ApplyTransactionTypeFromCostCode(ledgerLine, costCodeById);
        }

        await TryAddCompanyMarkupOrReferralLedgerLineAsync(reservation, lineItems, costCodeById);
        return (lineItems, rentalDaysCharged);
    }

    /// <summary>
    /// Same full-month switch as Get Charges (<see cref="AddRentalLine"/>): prorate while
    /// days are under both the calendar month and 30; otherwise the month is a full rent charge.
    /// </summary>
    private static int ResolveMonthlyExtensionDays(int daysAlreadyBilled, int gapDays, int daysInMonth)
    {
        var billed = Math.Max(0, daysAlreadyBilled);
        var combined = billed + Math.Max(0, gapDays);
        return MonthlyChargeDays(combined, daysInMonth) - MonthlyChargeDays(billed, daysInMonth);
    }

    private static int MonthlyChargeDays(int days, int daysInMonth)
    {
        if (days <= 0)
            return 0;
        if (days < daysInMonth && days < PRORATE_DAYS)
            return days;
        return PRORATE_DAYS;
    }

    private static int SumRentalDaysBilledInMonth(IReadOnlyList<Invoice> invoices, Reservation reservation, DateOnly billingMonth)
    {
        var total = 0;
        foreach (var invoice in invoices)
            total += CountRentalDays(invoice.LedgerLines, reservation, billingMonth);
        return total;
    }

    private static int CountRentalDays(IReadOnlyList<LedgerLine>? lines, Reservation reservation, DateOnly billingMonth)
    {
        if (lines == null || lines.Count == 0)
            return 0;

        var monthStart = new DateOnly(billingMonth.Year, billingMonth.Month, 1);
        var monthEnd = LastDayOfMonth(billingMonth);
        var departureDate = ResolveBillingDepartureDate(reservation);
        var total = 0;
        foreach (var line in lines)
        {
            if (line.Amount == 0)
                continue;
            if (!InvoiceBillingDays.TryParseRentalFeePeriod(line.Description, billingMonth.Year, out var periodStart, out var periodEnd))
                continue;
            if (periodEnd < monthStart || periodStart > monthEnd)
                continue;

            var clippedStart = periodStart < monthStart ? monthStart : periodStart;
            var clippedEnd = periodEnd > monthEnd ? monthEnd : periodEnd;
            total += CalculateNumberOfDays(
                clippedStart,
                clippedEnd,
                reservation.BillingType,
                clippedEnd.Month == departureDate.Month && clippedEnd.Year == departureDate.Year,
                clippedEnd.Day == monthEnd.Day);
        }

        return total;
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

    /// <summary>Missing/preview billed grid only — physical stay through calendar month-end.</summary>
    private static (DateOnly PeriodStart, DateOnly PeriodEnd) ResolveBilledGridPeriodForMonth(Reservation reservation, DateOnly billingMonth)
    {
        var monthStart = billingMonth;
        var monthEnd = LastDayOfMonth(billingMonth);
        var arrivalDate = ResolveBillingArrivalDate(reservation);
        var stayDeparture = reservation.DepartureDate;
        var periodStart = arrivalDate > monthStart ? arrivalDate : monthStart;
        if (stayDeparture < periodStart)
            return (periodStart, periodStart.AddDays(-1));

        var periodEnd = stayDeparture > monthEnd ? monthEnd : stayDeparture;
        return (periodStart, periodEnd);
    }

    private static IEnumerable<DateOnly> EnumerateBillableMonthsForBilledGrid(
        Reservation reservation,
        DateOnly throughMonth,
        DateOnly accountingStartMonth)
    {
        var arrivalDate = ResolveBillingArrivalDate(reservation);
        var stayDeparture = reservation.DepartureDate;
        var startMonth = FirstDayOfMonth(arrivalDate);
        if (startMonth < accountingStartMonth)
            startMonth = accountingStartMonth;

        var departureMonth = FirstDayOfMonth(stayDeparture);
        var scanThrough = throughMonth <= departureMonth ? throughMonth : departureMonth;

        if (startMonth > scanThrough)
            yield break;

        for (var month = startMonth; month <= scanThrough; month = month.AddMonths(1))
        {
            if (!ReservationOverlapsBillingMonth(arrivalDate, stayDeparture, month, LastDayOfMonth(month)))
                continue;

            var (periodStart, periodEnd) = ResolveBilledGridPeriodForMonth(reservation, month);
            if (!HasBillablePreviewPeriod(reservation, periodStart, periodEnd))
                continue;

            yield return month;
        }
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

    private static void ApplyMissingPreviewBilledGridMetrics(Invoice preview, Billed billedRow)
    {
        preview.BilledMonthStart = billedRow.MonthStart;
        preview.BilledMonthEnd = billedRow.MonthEnd;
        preview.BilledPeriodStart = billedRow.PeriodStart;
        preview.BilledPeriodEnd = billedRow.PeriodEnd;
        preview.BilledStartDate = billedRow.StartDate;
        preview.BilledEndDate = billedRow.EndDate;
        preview.BilledDaysStayed = billedRow.DaysStayed;
        preview.BilledDaysBilled = billedRow.DaysBilled;
    }

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
            TenantName = NormalizeOptionalString(reservation.TenantName)
                ?? NormalizeOptionalString(reservation.ContactName),
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

    #region Company Markup
    private async Task TryAddCompanyMarkupOrReferralLedgerLineAsync(Reservation reservation, List<LedgerLine> ledgerLines, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (ReservationHasReferralFee(reservation))
        {
            RemoveCompanyMarkupLedgerLines(ledgerLines);
            await TryAddCompanyReferralLedgerLineAsync(reservation, ledgerLines, costCodeById);
            return;
        }

        RemoveCompanyReferralLedgerLines(ledgerLines);
        await TryAddCompanyMarkupLedgerLineAsync(reservation, ledgerLines, costCodeById);
    }

    private async Task TryAddCompanyMarkupLedgerLineAsync(Reservation reservation, List<LedgerLine> ledgerLines, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var companyId = NormalizeOptionalGuid(reservation.CompanyId);
        if (companyId == null || ledgerLines.Count == 0)
            return;

        var rentLines = ledgerLines.Where(IsRentalFeeLedgerLine).ToList();
        if (rentLines.Count == 0)
            return;

        var companyContact = await _contactRepository.GetContactByIdsAsync(companyId.Value, reservation.OrganizationId);
        var markupPercent = companyContact?.Markup ?? 0;
        if (markupPercent <= 0)
        {
            RemoveCompanyMarkupLedgerLines(ledgerLines);
            return;
        }

        var rentAmount = rentLines.Sum(line => line.Amount);
        var markupAmount = Math.Round(rentAmount * markupPercent / 100m, 2, MidpointRounding.AwayFromZero);
        if (markupAmount == 0)
        {
            RemoveCompanyMarkupLedgerLines(ledgerLines);
            return;
        }

        var rentLine = rentLines[^1];
        var markupLine = new LedgerLine
        {
            ReservationId = reservation.ReservationId,
            CostCodeId = rentLine.CostCodeId,
            Amount = markupAmount,
            Description = $"Company Markup {markupPercent}%",
            LedgerLineDate = rentLine.LedgerLineDate
        };
        ApplyTransactionTypeFromCostCode(markupLine, costCodeById);
        UpsertCompanyMarkupLedgerLine(ledgerLines, rentLines, markupLine);
    }

    private async Task EnsureCompanyMarkupLedgerLineOnInvoiceAsync(Invoice invoice)
    {
        if (invoice.ReservationId is not { } reservationId || reservationId == Guid.Empty)
            return;

        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, invoice.OrganizationId);
        if (reservation == null)
            return;

        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var amountBefore = invoice.LedgerLines.Sum(line => line.Amount);
        await TryAddCompanyMarkupOrReferralLedgerLineAsync(reservation, invoice.LedgerLines, costCodeById);
        var delta = invoice.LedgerLines.Sum(line => line.Amount) - amountBefore;
        if (delta != 0)
            invoice.TotalAmount += delta;
    }

    private static bool IsCompanyMarkupLedgerLine(LedgerLine line)
        => line.Description.StartsWith("Company Markup", StringComparison.Ordinal);

    private static void UpsertCompanyMarkupLedgerLine(List<LedgerLine> ledgerLines, List<LedgerLine> rentLines, LedgerLine templateLine)
    {
        UpsertCompanyLedgerLine(ledgerLines, rentLines, templateLine, IsCompanyMarkupLedgerLine);
    }

    private static void RemoveCompanyMarkupLedgerLines(List<LedgerLine> ledgerLines)
    {
        if (ledgerLines.RemoveAll(IsCompanyMarkupLedgerLine) == 0)
            return;

        RenumberLedgerLines(ledgerLines);
    }

    private static void UpsertCompanyLedgerLine(List<LedgerLine> ledgerLines, List<LedgerLine> rentLines, LedgerLine templateLine, Func<LedgerLine, bool> isCompanyMarkupOrReferralLine)
    {
        var rentLine = rentLines[^1];
        var companyLines = ledgerLines.Where(isCompanyMarkupOrReferralLine).ToList();
        if (companyLines.Count == 0)
        {
            var insertIndex = ledgerLines.IndexOf(rentLine);
            insertIndex = insertIndex < 0 ? ledgerLines.Count : insertIndex + 1;
            ledgerLines.Insert(insertIndex, templateLine);
            RenumberLedgerLines(ledgerLines);
            return;
        }

        var primary = companyLines[0];
        primary.ReservationId = templateLine.ReservationId;
        primary.CostCodeId = templateLine.CostCodeId;
        primary.Amount = templateLine.Amount;
        primary.Description = templateLine.Description;
        primary.LedgerLineDate = templateLine.LedgerLineDate;
        primary.TransactionType = templateLine.TransactionType;

        if (companyLines.Count > 1)
        {
            for (var duplicateIndex = 1; duplicateIndex < companyLines.Count; duplicateIndex++)
                ledgerLines.Remove(companyLines[duplicateIndex]);
            RenumberLedgerLines(ledgerLines);
        }
    }

    private static void RenumberLedgerLines(List<LedgerLine> ledgerLines)
    {
        for (var i = 0; i < ledgerLines.Count; i++)
            ledgerLines[i].LineNumber = i + 1;
    }
    #endregion

    #region Company Referral
    private static bool ReservationHasReferralFee(Reservation reservation)
        => reservation.ReferralFee && reservation.ReferralMethod != ReferralMethodType.None;

    private async Task TryAddCompanyReferralLedgerLineAsync(Reservation reservation, List<LedgerLine> ledgerLines, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (!ReservationHasReferralFee(reservation) || ledgerLines.Count == 0)
            return;

        var rentLines = ledgerLines.Where(IsRentalFeeLedgerLine).ToList();
        if (rentLines.Count == 0)
            return;

        var rentAmount = rentLines.Sum(line => line.Amount);
        if (!TryResolveReferralFeeAmount(reservation, rentAmount, out var referralAmount))
        {
            RemoveCompanyReferralLedgerLines(ledgerLines);
            return;
        }

        switch (reservation.ReferralMethod)
        {
            case ReferralMethodType.NetInvoice:
                await AddReferralNetInvoiceLedgerLinesAsync(reservation, ledgerLines, rentLines, referralAmount, costCodeById);
                break;
            case ReferralMethodType.SeparateInvoice:
                await AddReferralSeparateInvoiceLedgerLinesAsync(reservation, ledgerLines, rentLines, referralAmount, costCodeById);
                break;
            case ReferralMethodType.Bill:
                await AddReferralBillLedgerLinesAsync(reservation, ledgerLines, rentLines, referralAmount, costCodeById);
                break;
            default:
                break;
        }
    }

    private Task AddReferralNetInvoiceLedgerLinesAsync(Reservation reservation, List<LedgerLine> ledgerLines, List<LedgerLine> rentLines, decimal referralAmount, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var expectedDescriptions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rentLine in rentLines)
        {
            var credit = CalculateReferralFeeAmountForRentLine(reservation, rentLine);
            if (credit <= 0)
                continue;

            var referralDescription = BuildReferralFeeDescriptionFromRentalFeeLine(rentLine.Description);
            if (string.IsNullOrEmpty(referralDescription))
                continue;

            expectedDescriptions.Add(referralDescription);

            var referralLine = new LedgerLine
            {
                ReservationId = reservation.ReservationId,
                CostCodeId = rentLine.CostCodeId,
                Amount = -credit,
                Description = referralDescription,
                LedgerLineDate = rentLine.LedgerLineDate
            };
            ApplyTransactionTypeFromCostCode(referralLine, costCodeById);
            UpsertNetInvoiceReferralLineAfterRentLine(ledgerLines, rentLine, referralLine);
        }

        RemoveNetInvoiceReferralLinesNotInExpectedDescriptions(ledgerLines, expectedDescriptions);
        return Task.CompletedTask;
    }

    private static bool UsesReferralPercentage(Reservation reservation)
        => reservation.ReferralPercentage > 0;

    private static bool UsesReferralFlatRate(Reservation reservation)
        => !UsesReferralPercentage(reservation) && reservation.ReferralFlatRate > 0;

    /// <summary>Referral % is always a percentage of the rental fee line amount from Get Charges.</summary>
    private static decimal CalculateReferralPercentageOfRent(decimal rentAmount, decimal referralPercentage)
        => Math.Round(rentAmount * referralPercentage / 100m, 2, MidpointRounding.AwayFromZero);

    private decimal CalculateReferralFeeAmountForRentLine(Reservation reservation, LedgerLine rentLine)
    {
        var rentAmount = rentLine.Amount;
        if (UsesReferralPercentage(reservation))
            return CalculateReferralPercentageOfRent(rentAmount, reservation.ReferralPercentage);

        if (!UsesReferralFlatRate(reservation))
            return 0;

        var referenceYear = rentLine.LedgerLineDate.Year;
        if (!RentalFeeLineParser.TryParseRentalFeePeriod(rentLine.Description, referenceYear, out var periodStart, out var periodEnd))
            return reservation.ReferralFlatRate;

        var departureDate = ResolveBillingDepartureDate(reservation);
        var isDepartureMonthYear = InvoiceBillingDays.IsDepartureMonthYear(periodEnd, departureDate);
        var isLastDayOfMonth = InvoiceBillingDays.IsLastDayOfMonth(periodEnd);
        var days = InvoiceBillingDays.CountRentalFeeDescriptionDays(
            rentLine.Description,
            referenceYear,
            reservation.BillingType,
            isDepartureMonthYear,
            isLastDayOfMonth,
            departureDate,
            periodEnd);
        if (days <= 0)
            return 0;

        if (reservation.BillingType == BillingType.Monthly)
        {
            var daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
            if (!ShouldProrateMonthlyReferralFlatRate(days, daysInMonth))
                return reservation.ReferralFlatRate;
            return Math.Round((reservation.ReferralFlatRate / PRORATE_DAYS) * days, 2, MidpointRounding.AwayFromZero);
        }

        // Daily / Nightly: flat rate is per day (same semantics as billing rate on rental lines).
        return Math.Round(reservation.ReferralFlatRate * days, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Same partial-month switch as <see cref="AddRentalLine"/> for monthly billing.</summary>
    private static bool ShouldProrateMonthlyReferralFlatRate(int days, int daysInMonth)
        => days < daysInMonth && days < PRORATE_DAYS;

    private static string? BuildReferralFeeDescriptionFromRentalFeeLine(string? rentalFeeDescription)
    {
        const string rentalPrefix = "Rental Fee ";
        if (string.IsNullOrWhiteSpace(rentalFeeDescription) || !rentalFeeDescription.StartsWith(rentalPrefix, StringComparison.Ordinal))
            return null;

        return "Referral Fee " + rentalFeeDescription[rentalPrefix.Length..];
    }

    private static void UpsertNetInvoiceReferralLineAfterRentLine(List<LedgerLine> ledgerLines, LedgerLine rentLine, LedgerLine templateLine)
    {
        var existing = ledgerLines.FirstOrDefault(line =>
            string.Equals(line.Description, templateLine.Description, StringComparison.Ordinal));

        if (existing != null)
        {
            existing.ReservationId = templateLine.ReservationId;
            existing.CostCodeId = templateLine.CostCodeId;
            existing.Amount = templateLine.Amount;
            existing.LedgerLineDate = templateLine.LedgerLineDate;
            existing.TransactionType = templateLine.TransactionType;
            EnsureReferralLineFollowsRentLine(ledgerLines, rentLine, existing);
            return;
        }

        var insertIndex = ledgerLines.IndexOf(rentLine);
        insertIndex = insertIndex < 0 ? ledgerLines.Count : insertIndex + 1;
        ledgerLines.Insert(insertIndex, templateLine);
        RenumberLedgerLines(ledgerLines);
    }

    private static void EnsureReferralLineFollowsRentLine(List<LedgerLine> ledgerLines, LedgerLine rentLine, LedgerLine referralLine)
    {
        var rentIndex = ledgerLines.IndexOf(rentLine);
        var referralIndex = ledgerLines.IndexOf(referralLine);
        if (rentIndex < 0 || referralIndex < 0 || referralIndex == rentIndex + 1)
            return;

        ledgerLines.RemoveAt(referralIndex);
        rentIndex = ledgerLines.IndexOf(rentLine);
        ledgerLines.Insert(rentIndex + 1, referralLine);
        RenumberLedgerLines(ledgerLines);
    }

    private static void RemoveNetInvoiceReferralLinesNotInExpectedDescriptions(List<LedgerLine> ledgerLines, HashSet<string> expectedDescriptions)
    {
        var removed = ledgerLines.RemoveAll(line =>
            IsCompanyReferralLedgerLine(line)
            && !expectedDescriptions.Contains(line.Description ?? string.Empty));
        if (removed > 0)
            RenumberLedgerLines(ledgerLines);
    }

    private Task AddReferralSeparateInvoiceLedgerLinesAsync(Reservation reservation, List<LedgerLine> ledgerLines, List<LedgerLine> rentLines, decimal referralAmount, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        RemoveCompanyReferralLedgerLines(ledgerLines);
        return Task.CompletedTask;
    }

    private static bool IsReferralSeparateInvoiceDocument(Invoice invoice)
    {
        if (invoice.LedgerLines == null || invoice.LedgerLines.Count == 0)
            return false;

        return invoice.LedgerLines.All(line => IsCompanyReferralLedgerLine(line));
    }

    private List<LedgerLine> BuildReferralSeparateInvoiceLedgerLines(
        Reservation reservation,
        IReadOnlyList<LedgerLine> rentLines,
        IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var lines = new List<LedgerLine>();
        var lineNumber = 1;
        foreach (var rentLine in rentLines)
        {
            var amount = CalculateReferralFeeAmountForRentLine(reservation, rentLine);
            if (amount <= 0)
                continue;

            var referralDescription = BuildReferralFeeDescriptionFromRentalFeeLine(rentLine.Description);
            if (string.IsNullOrEmpty(referralDescription))
                continue;

            var referralLine = new LedgerLine
            {
                LineNumber = lineNumber++,
                ReservationId = reservation.ReservationId,
                CostCodeId = rentLine.CostCodeId,
                Amount = -amount,
                Description = referralDescription,
                LedgerLineDate = rentLine.LedgerLineDate
            };
            ApplyTransactionTypeFromCostCode(referralLine, costCodeById);
            lines.Add(referralLine);
        }

        return lines;
    }

    private async Task StripReferralLinesUnlessNetInvoiceAsync(Invoice invoice)
    {
        if (IsReferralSeparateInvoiceDocument(invoice))
            return;

        if (invoice.ReservationId is { } reservationId && reservationId != Guid.Empty)
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, invoice.OrganizationId);
            if (reservation != null && ReservationHasReferralFee(reservation) && reservation.ReferralMethod == ReferralMethodType.NetInvoice)
                return;
        }

        StripReferralLinesFromMainInvoiceAndAdjustTotal(invoice);
    }

    private void StripReferralLinesFromMainInvoiceAndAdjustTotal(Invoice invoice)
    {
        if (invoice.LedgerLines == null || invoice.LedgerLines.Count == 0)
            return;

        if (IsReferralSeparateInvoiceDocument(invoice))
            return;

        var removedAmount = invoice.LedgerLines.Where(IsCompanyReferralLedgerLine).Sum(line => line.Amount);
        if (removedAmount == 0)
            return;

        RemoveCompanyReferralLedgerLines(invoice.LedgerLines);
        invoice.TotalAmount -= removedAmount;
    }

    private async Task SyncReferralSeparateInvoiceForMainInvoiceAsync(Invoice mainInvoice, Guid currentUser)
    {
        if (mainInvoice.ReservationId is not { } reservationId || reservationId == Guid.Empty)
            return;

        if (IsReferralSeparateInvoiceDocument(mainInvoice))
            return;

        StripReferralLinesFromMainInvoiceAndAdjustTotal(mainInvoice);

        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, mainInvoice.OrganizationId);
        if (reservation == null
            || !ReservationHasReferralFee(reservation)
            || reservation.ReferralMethod != ReferralMethodType.SeparateInvoice)
        {
            await TryDeleteReferralSeparateInvoiceForMainAsync(mainInvoice, currentUser);
            return;
        }

        var costCodeById = await LoadCostCodeByOfficeIdAsync(mainInvoice.OrganizationId, mainInvoice.OfficeId);
        var rentLines = mainInvoice.LedgerLines.Where(IsRentalFeeLedgerLine).ToList();
        if (rentLines.Count == 0)
        {
            await TryDeleteReferralSeparateInvoiceForMainAsync(mainInvoice, currentUser);
            return;
        }

        var rentAmount = rentLines.Sum(line => line.Amount);
        if (!TryResolveReferralFeeAmount(reservation, rentAmount, out _))
        {
            await TryDeleteReferralSeparateInvoiceForMainAsync(mainInvoice, currentUser);
            return;
        }

        var referralLines = BuildReferralSeparateInvoiceLedgerLines(reservation, rentLines, costCodeById);
        if (referralLines.Count == 0)
        {
            await TryDeleteReferralSeparateInvoiceForMainAsync(mainInvoice, currentUser);
            return;
        }

        var existingCompanion = await FindReferralSeparateInvoiceForMainAsync(mainInvoice);
        if (existingCompanion != null)
        {
            existingCompanion.LedgerLines = referralLines;
            existingCompanion.TotalAmount = referralLines.Sum(line => line.Amount);
            existingCompanion.InvoiceDate = mainInvoice.InvoiceDate;
            existingCompanion.DueDate = mainInvoice.DueDate;
            existingCompanion.AccountingPeriod = mainInvoice.AccountingPeriod;
            existingCompanion.InvoicePeriod = mainInvoice.InvoicePeriod;
            existingCompanion.ModifiedBy = currentUser;
            await UpdateInvoiceAsync(existingCompanion);
            return;
        }

        var invoiceCode = await ResolveNextReservationInvoiceCodeAsync(mainInvoice.OrganizationId, reservation);
        var companion = BuildReferralSeparateInvoiceFromMain(mainInvoice, reservation, referralLines, invoiceCode, currentUser);
        await CreateInvoiceAsync(companion, currentUser);
    }

    private async Task TryDeleteReferralSeparateInvoiceForMainAsync(Invoice mainInvoice, Guid currentUser)
    {
        var companion = await FindReferralSeparateInvoiceForMainAsync(mainInvoice);
        if (companion == null)
            return;

        await DeleteInvoiceAsync(companion.InvoiceId, mainInvoice.OrganizationId, currentUser);
    }

    private async Task<Invoice?> FindReferralSeparateInvoiceForMainAsync(Invoice mainInvoice)
    {
        if (mainInvoice.ReservationId is not { } reservationId || reservationId == Guid.Empty)
            return null;

        var invoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = mainInvoice.OrganizationId,
            OfficeIds = mainInvoice.OfficeId.ToString(),
            ReservationId = reservationId,
            IsActive = true,
            IncludePaid = true
        });

        return invoices
            .Where(invoice => invoice.InvoiceId != mainInvoice.InvoiceId)
            .Where(invoice => invoice.AccountingPeriod == mainInvoice.AccountingPeriod)
            .FirstOrDefault(IsReferralSeparateInvoiceDocument);
    }

    private async Task<string> ResolveNextReservationInvoiceCodeAsync(Guid organizationId, Reservation reservation)
    {
        var existingInvoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(),
            ReservationId = reservation.ReservationId,
            IsActive = true,
            IncludePaid = true
        });

        var nextSequence = ResolveNextInvoiceSequence(reservation, existingInvoices.ToList());
        return $"{reservation.ReservationCode}-{nextSequence:000}";
    }

    private static Invoice BuildReferralSeparateInvoiceFromMain(
        Invoice mainInvoice,
        Reservation reservation,
        List<LedgerLine> referralLines,
        string invoiceCode,
        Guid currentUser)
    {
        for (var lineIndex = 0; lineIndex < referralLines.Count; lineIndex++)
            referralLines[lineIndex].LineNumber = lineIndex + 1;

        return new Invoice
        {
            OrganizationId = mainInvoice.OrganizationId,
            OfficeId = mainInvoice.OfficeId,
            OfficeName = mainInvoice.OfficeName,
            InvoiceCode = invoiceCode,
            ReservationId = reservation.ReservationId,
            ReservationCode = reservation.ReservationCode,
            PropertyId = mainInvoice.PropertyId,
            PropertyCode = mainInvoice.PropertyCode,
            ContactId = mainInvoice.ContactId,
            ContactName = mainInvoice.ContactName,
            TenantName = mainInvoice.TenantName,
            CompanyId = mainInvoice.CompanyId,
            CompanyName = mainInvoice.CompanyName,
            ResponsibleParty = mainInvoice.ResponsibleParty,
            InvoiceDate = mainInvoice.InvoiceDate,
            DueDate = mainInvoice.DueDate,
            AccountingPeriod = mainInvoice.AccountingPeriod,
            InvoicePeriod = mainInvoice.InvoicePeriod,
            TotalAmount = referralLines.Sum(line => line.Amount),
            PaidAmount = 0,
            IsActive = true,
            CreatedBy = currentUser,
            ModifiedBy = currentUser,
            LedgerLines = referralLines
        };
    }

    private void TryAppendReferralSeparateInvoicePreview(
        Reservation reservation,
        Invoice mainPreview,
        List<Invoice> previewInvoices,
        ref int nextSequence,
        IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (!ReservationHasReferralFee(reservation) || reservation.ReferralMethod != ReferralMethodType.SeparateInvoice)
            return;

        var rentLines = mainPreview.LedgerLines.Where(IsRentalFeeLedgerLine).ToList();
        if (rentLines.Count == 0)
            return;

        var rentAmount = rentLines.Sum(line => line.Amount);
        if (!TryResolveReferralFeeAmount(reservation, rentAmount, out _))
            return;

        var referralLines = BuildReferralSeparateInvoiceLedgerLines(reservation, rentLines, costCodeById);
        if (referralLines.Count == 0)
            return;

        var referralTotal = referralLines.Sum(line => line.Amount);
        var periodStart = mainPreview.BilledPeriodStart ?? mainPreview.InvoiceDate;
        var periodEnd = mainPreview.BilledPeriodEnd ?? mainPreview.InvoiceDate;
        var referralPreview = BuildPreBillingInvoicePreview(
            mainPreview.OrganizationId,
            reservation,
            mainPreview.AccountingPeriod,
            periodStart,
            periodEnd,
            referralLines,
            referralTotal,
            nextSequence,
            mainPreview.ResponsibleParty);
        referralPreview.InvoicePeriod = mainPreview.InvoicePeriod;
        previewInvoices.Add(referralPreview);
        nextSequence++;
    }

    private Task AddReferralBillLedgerLinesAsync(Reservation reservation, List<LedgerLine> ledgerLines, List<LedgerLine> rentLines, decimal referralAmount, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        RemoveCompanyReferralLedgerLines(ledgerLines);
        return Task.CompletedTask;
    }

    private static string BuildReferralBillDescription(Reservation reservation)
    {
        var reservationCode = (reservation.ReservationCode ?? string.Empty).Trim();
        return string.IsNullOrEmpty(reservationCode)
            ? "Referral Fee"
            : $"Referral Fee for {reservationCode}";
    }

    private static bool IsReferralBillDocument(Receipt bill)
    {
        if (bill.BankCardId is > 0)
            return false;

        var description = bill.Description?.Trim() ?? string.Empty;
        return description.StartsWith("Referral Fee", StringComparison.OrdinalIgnoreCase);
    }

    private decimal CalculateReferralBillAmountForRentLines(Reservation reservation, IReadOnlyList<LedgerLine> rentLines)
        => rentLines.Sum(rentLine => CalculateReferralFeeAmountForRentLine(reservation, rentLine));

    private async Task<List<ReceiptSplit>> BuildReferralBillSplitsAsync(
        Invoice mainInvoice,
        Reservation reservation,
        IReadOnlyList<LedgerLine> rentLines,
        decimal amount,
        string splitDescription)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(mainInvoice.OrganizationId, mainInvoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(mainInvoice.OrganizationId, mainInvoice.OfficeId);
        CostCode? rentCostCode = null;
        foreach (var rentLine in rentLines)
        {
            if (costCodeById.TryGetValue(rentLine.CostCodeId, out var costCode))
            {
                rentCostCode = costCode;
                break;
            }
        }

        var companyExpenseAccountId = GetDefaultCompanyExpense(chartOfAccounts, mainInvoice.OfficeId, accountingOffice, rentCostCode);
        return
        [
            new ReceiptSplit
            {
                Amount = amount,
                Description = splitDescription,
                PropertyId = ResolveReferralBillSplitPropertyId(mainInvoice, reservation),
                ReceiptTypeId = (int)ReceiptType.Company,
                ChartOfAccountId = companyExpenseAccountId > 0 ? companyExpenseAccountId : null,
                ChartOfAccountDisplayName = companyExpenseAccountId > 0 ? "Company" : string.Empty
            }
        ];
    }

    private async Task<Invoice> GetInvoiceForReferralDocumentSyncAsync(Invoice invoice)
    {
        if (invoice.InvoiceId == Guid.Empty)
            return invoice;

        return await _accountingRepository.GetInvoiceByIdAsync(invoice.InvoiceId, invoice.OrganizationId)
            ?? invoice;
    }

    private async Task<(bool BillCreated, bool VendorCreated)> SyncReferralBillForMainInvoiceAsync(Invoice mainInvoice, Guid currentUser)
    {
        if (mainInvoice.ReservationId is not { } reservationId || reservationId == Guid.Empty)
        {
            LogReferralBillSyncSkipped(mainInvoice, "MissingReservationId");
            return (false, false);
        }

        if (IsReferralSeparateInvoiceDocument(mainInvoice))
        {
            LogReferralBillSyncSkipped(mainInvoice, "ReferralOnlyInvoice");
            return (false, false);
        }

        StripReferralLinesFromMainInvoiceAndAdjustTotal(mainInvoice);

        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, mainInvoice.OrganizationId);
        if (reservation == null || !ReservationHasReferralFee(reservation))
        {
            await TryDeleteReferralBillForMainAsync(mainInvoice, reservation, currentUser);
            return (false, false);
        }

        if (reservation.ReferralMethod != ReferralMethodType.Bill)
        {
            await TryDeleteReferralBillForMainAsync(mainInvoice, reservation, currentUser);
            return (false, false);
        }

        var (vendor, vendorCreated) = await ResolveReferralBillVendorAsync(reservation, mainInvoice, currentUser);
        var rentLines = mainInvoice.LedgerLines.Where(IsRentalFeeLedgerLine).ToList();
        if (rentLines.Count == 0)
        {
            await TryDeleteReferralBillForMainAsync(mainInvoice, reservation, currentUser);
            LogReferralBillSyncSkipped(mainInvoice, "NoRentalFeeLines", reservation.ReservationCode);
            return (false, false);
        }

        var rentAmount = rentLines.Sum(line => line.Amount);
        if (!TryResolveReferralFeeAmount(reservation, rentAmount, out _))
        {
            await TryDeleteReferralBillForMainAsync(mainInvoice, reservation, currentUser);
            LogReferralBillSyncSkipped(mainInvoice, "ReferralAmountNotConfigured", reservation.ReservationCode);
            return (false, false);
        }

        var referralAmount = CalculateReferralBillAmountForRentLines(reservation, rentLines);
        if (referralAmount <= 0)
        {
            await TryDeleteReferralBillForMainAsync(mainInvoice, reservation, currentUser);
            LogReferralBillSyncSkipped(mainInvoice, "ReferralAmountZero", reservation.ReservationCode);
            return (false, false);
        }

        var referralDescription = BuildReferralBillDescription(reservation);
        List<ReceiptSplit> splits;
        try
        {
            splits = await BuildReferralBillSplitsAsync(mainInvoice, reservation, rentLines, referralAmount, referralDescription);
        }
        catch (Exception ex)
        {
            LogApplicationDiagnostic(
                "ReferralBillMissingCompanyExpenseAccount",
                $"ReservationCode={reservation.ReservationCode} InvoiceCode={mainInvoice.InvoiceCode}",
                ex,
                mainInvoice.OrganizationId,
                mainInvoice.OfficeId);
            return (false, false);
        }

        if (splits.Count == 0 || splits[0].ChartOfAccountId is not > 0)
        {
            LogApplicationDiagnostic(
                "ReferralBillMissingCompanyExpenseAccount",
                $"ReservationCode={reservation.ReservationCode} InvoiceCode={mainInvoice.InvoiceCode}",
                null,
                mainInvoice.OrganizationId,
                mainInvoice.OfficeId);
            return (false, false);
        }

        var vendorName = vendor == null ? null : NormalizeOptionalString(vendor.CompanyName);
        var existingBill = await FindReferralBillForMainInvoiceAsync(mainInvoice, reservation);
        if (existingBill != null)
        {
            existingBill.Amount = referralAmount;
            existingBill.Description = referralDescription;
            existingBill.VendorId = vendor?.ContactId;
            existingBill.VendorName = vendorName;
            existingBill.ReceiptDate = mainInvoice.InvoiceDate;
            existingBill.DueDate = mainInvoice.DueDate == default ? mainInvoice.InvoiceDate : mainInvoice.DueDate;
            existingBill.AccountingPeriod = mainInvoice.AccountingPeriod;
            existingBill.BillNumber = mainInvoice.InvoiceCode?.Trim() ?? string.Empty;
            existingBill.PropertyIds = ResolveReferralBillPropertyIds(mainInvoice, reservation);
            existingBill.Splits = splits;
            existingBill.ModifiedBy = currentUser;
            await UpdateBillAsync(existingBill, currentUser);
            return (true, false);
        }

        var billCode = await _organizationManager.GenerateEntityCodeAsync(mainInvoice.OrganizationId, EntityType.Receipt);
        if (string.IsNullOrWhiteSpace(billCode))
            throw new Exception("Unable to generate referral bill code");

        var bill = new Receipt
        {
            OrganizationId = mainInvoice.OrganizationId,
            OfficeId = mainInvoice.OfficeId,
            OfficeName = mainInvoice.OfficeName,
            ReceiptCode = billCode.Trim(),
            PropertyIds = ResolveReferralBillPropertyIds(mainInvoice, reservation),
            ReceiptDate = mainInvoice.InvoiceDate,
            DueDate = mainInvoice.DueDate == default ? mainInvoice.InvoiceDate : mainInvoice.DueDate,
            AccountingPeriod = mainInvoice.AccountingPeriod,
            BillNumber = mainInvoice.InvoiceCode?.Trim() ?? string.Empty,
            Amount = referralAmount,
            Description = referralDescription,
            BankCardId = null,
            VendorId = vendor?.ContactId,
            VendorName = vendorName,
            PaidAmount = 0,
            Splits = splits,
            PaymentTypeId = 0,
            IsActive = true,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        await CreateReceiptAsync(bill, currentUser);
        return (true, vendorCreated);
    }

    private void LogReferralBillSyncSkipped(Invoice mainInvoice, string reason, string? reservationCode = null)
    {
        LogApplicationDiagnostic(
            "ReferralBillSyncSkipped",
            $"Reason={reason} ReservationCode={reservationCode ?? mainInvoice.ReservationCode ?? string.Empty} InvoiceCode={mainInvoice.InvoiceCode}",
            null,
            mainInvoice.OrganizationId,
            mainInvoice.OfficeId);
    }

    private async Task<string?> ResolveReferralCompanyNameAsync(Reservation reservation, Invoice mainInvoice)
    {
        var companyId = NormalizeOptionalGuid(reservation.CompanyId) ?? NormalizeOptionalGuid(mainInvoice.CompanyId);
        if (companyId != null)
        {
            var companyContact = await _contactRepository.GetContactByIdsAsync(companyId.Value, reservation.OrganizationId);
            var companyName = NormalizeOptionalString(companyContact?.CompanyName);
            if (companyName != null)
                return companyName;
        }

        return NormalizeOptionalString(reservation.CompanyName) ?? NormalizeOptionalString(mainInvoice.CompanyName);
    }

    private async Task<(Contact? Vendor, bool Created)> ResolveReferralBillVendorAsync(Reservation reservation, Invoice mainInvoice, Guid currentUser)
    {
        var companyName = await ResolveReferralCompanyNameAsync(reservation, mainInvoice);
        if (companyName == null)
            return (null, false);

        var contacts = await _contactRepository.GetContactsByOrganizationIdAsync(reservation.OrganizationId);
        var vendors = contacts
            .Where(contact => contact.IsActive && contact.EntityType == EntityType.Vendor)
            .Where(contact => string.Equals(contact.CompanyName?.Trim(), companyName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (vendors.Count == 0)
        {
            var createdVendor = await CreateReferralVendorFromCompanyAsync(reservation, mainInvoice, currentUser);
            return (createdVendor, createdVendor != null);
        }

        var officeId = mainInvoice.OfficeId > 0 ? mainInvoice.OfficeId : reservation.OfficeId;
        return (vendors.FirstOrDefault(contact => contact.OfficeId == officeId || contact.OfficeAccess.Contains(officeId)) ?? vendors[0], false);
    }

    private async Task<Contact?> CreateReferralVendorFromCompanyAsync(Reservation reservation, Invoice mainInvoice, Guid currentUser)
    {
        var companyId = NormalizeOptionalGuid(reservation.CompanyId) ?? NormalizeOptionalGuid(mainInvoice.CompanyId);
        if (companyId == null)
            return null;

        var company = await _contactRepository.GetContactByIdsAsync(companyId.Value, reservation.OrganizationId);
        if (company == null)
            return null;

        var vendorCode = await _organizationManager.GenerateEntityCodeAsync(reservation.OrganizationId, EntityType.Vendor);
        if (string.IsNullOrWhiteSpace(vendorCode))
            return null;

        var officeId = mainInvoice.OfficeId > 0 ? mainInvoice.OfficeId : company.OfficeId;
        var officeAccess = company.OfficeAccess?.ToList() ?? new List<int>();
        if (officeId > 0 && !officeAccess.Contains(officeId))
            officeAccess.Add(officeId);

        return await _contactRepository.CreateAsync(new Contact
        {
            OrganizationId = company.OrganizationId,
            OfficeId = officeId > 0 ? officeId : company.OfficeId,
            OfficeAccess = officeAccess,
            ContactCode = vendorCode.Trim(),
            EntityType = EntityType.Vendor,
            VendorType = VendorType.Company,
            Properties = company.Properties?.ToList() ?? new List<string>(),
            CompanyName = company.CompanyName,
            CompanyEmail = company.CompanyEmail,
            DisplayName = company.DisplayName,
            FirstName = company.FirstName,
            LastName = company.LastName,
            PreferredName = company.PreferredName,
            Address1 = company.Address1,
            Address2 = company.Address2,
            City = company.City,
            State = company.State,
            Zip = company.Zip,
            Phone = company.Phone,
            Extension = company.Extension,
            Email = company.Email ?? string.Empty,
            Rating = company.Rating,
            Notes = company.Notes,
            IsInternational = company.IsInternational,
            PaymentTerms = company.PaymentTerms,
            BankName = company.BankName,
            RoutingNumber = company.RoutingNumber,
            AccountNumber = company.AccountNumber,
            IsActive = true,
            CreatedBy = currentUser
        });
    }

    private static List<Guid> ResolveReferralBillPropertyIds(Invoice mainInvoice, Reservation reservation)
    {
        var splitPropertyId = ResolveReferralBillSplitPropertyId(mainInvoice, reservation);
        if (splitPropertyId is { } propertyId && propertyId != Guid.Empty)
            return [propertyId];

        return [ReceiptPropertyConstants.CompanyPropertyId];
    }

    private static Guid? ResolveReferralBillSplitPropertyId(Invoice mainInvoice, Reservation reservation)
    {
        if (mainInvoice.PropertyId is { } invoicePropertyId && invoicePropertyId != Guid.Empty
            && !ReceiptPropertyConstants.IsCompanyPropertyId(invoicePropertyId))
        {
            return invoicePropertyId;
        }

        if (reservation.PropertyId != Guid.Empty)
            return reservation.PropertyId;

        return null;
    }

    private async Task<int> TryDeleteReferralBillForMainAsync(Invoice mainInvoice, Reservation? reservation, Guid currentUser)
    {
        if (reservation == null && mainInvoice.ReservationId is { } reservationId && reservationId != Guid.Empty)
            reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, mainInvoice.OrganizationId);

        var bills = await FindReferralBillsForMainInvoiceAsync(mainInvoice, reservation);
        var deleted = 0;
        foreach (var bill in bills)
        {
            if (bill.PaidAmount != 0)
            {
                LogReferralBillSyncSkipped(mainInvoice, "BillAlreadyPaid", reservation?.ReservationCode);
                continue;
            }

            await DeleteReceiptAsync(bill.ReceiptId, mainInvoice.OrganizationId, currentUser);
            deleted++;
        }

        return deleted;
    }

    private async Task<Receipt?> FindReferralBillForMainInvoiceAsync(Invoice mainInvoice, Reservation reservation)
    {
        var bills = await FindReferralBillsForMainInvoiceAsync(mainInvoice, reservation);
        return bills.FirstOrDefault();
    }

    private async Task<List<Receipt>> FindReferralBillsForMainInvoiceAsync(Invoice mainInvoice, Reservation? reservation)
    {
        var invoiceCode = mainInvoice.InvoiceCode?.Trim();
        if (string.IsNullOrWhiteSpace(invoiceCode))
            return [];

        var officeIds = new List<int>();
        if (mainInvoice.OfficeId > 0)
            officeIds.Add(mainInvoice.OfficeId);
        if (reservation is { OfficeId: > 0 } && !officeIds.Contains(reservation.OfficeId))
            officeIds.Add(reservation.OfficeId);
        if (officeIds.Count == 0)
            return [];

        var bills = await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = mainInvoice.OrganizationId,
            OfficeIds = string.Join(",", officeIds),
            ReceiptKind = ReceiptKind.Bill,
            IsActive = true
        });

        return bills
            .Where(bill => EntityCodeFormatting.CodesMatch(bill.BillNumber, invoiceCode))
            .Where(IsReferralBillDocument)
            .ToList();
    }

    private static bool TryResolveReferralFeeAmount(Reservation reservation, decimal totalRentAmount, out decimal referralAmount)
    {
        referralAmount = 0;
        if (UsesReferralPercentage(reservation))
        {
            if (totalRentAmount <= 0)
                return false;

            referralAmount = CalculateReferralPercentageOfRent(totalRentAmount, reservation.ReferralPercentage);
            return referralAmount != 0;
        }

        if (UsesReferralFlatRate(reservation))
        {
            referralAmount = reservation.ReferralFlatRate;
            return totalRentAmount > 0;
        }

        return false;
    }

    private static bool IsCompanyReferralLedgerLine(LedgerLine line)
        => line.Description.StartsWith("Referral Fee", StringComparison.Ordinal);

    private static void UpsertCompanyReferralLedgerLine(List<LedgerLine> ledgerLines, List<LedgerLine> rentLines, LedgerLine templateLine)
    {
        UpsertCompanyLedgerLine(ledgerLines, rentLines, templateLine, IsCompanyReferralLedgerLine);
    }

    private static void RemoveCompanyReferralLedgerLines(List<LedgerLine> ledgerLines)
    {
        if (ledgerLines.RemoveAll(IsCompanyReferralLedgerLine) == 0)
            return;

        RenumberLedgerLines(ledgerLines);
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
        Payment? createdPayment = null;
        try
        {
            await EnsurePaymentCodeAsync(payment);
            createdPayment = await _accountingRepository.CreatePaymentAsync(payment);

            var invoicePayment = await ApplyPaymentToInvoicesAsync(invoiceIds.ToList(), payment.OrganizationId, officeAccess, payment.CostCodeId, payment.Description, payment.Amount, payment.PaymentDate, currentUser);

            await LinkInvoicePaymentApplicationsAsync(createdPayment.PaymentId, invoicePayment, currentUser);
            await CreateJournalEntriesFromInvoicePaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);
            await EnsurePaymentPostingStatusComplianceAsync(createdPayment, currentUser);
        }
        catch (Exception ex)
        {
            LogApplicationDiagnostic("CreatePaymentInvoice", ex.Message, ex, payment.OrganizationId, payment.OfficeId);
            if (createdPayment != null)
                await TryDeleteIncompletePaymentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

            throw;
        }

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment!.PaymentId, payment.OrganizationId)
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
            await EnsurePaymentPostingStatusComplianceAsync(createdPayment, currentUser);
        }
        catch (Exception ex)
        {
            LogApplicationDiagnostic("CreatePaymentInvoice", ex.Message, ex, payment.OrganizationId, payment.OfficeId);
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

        if (existing.PaymentKindId != (int)PaymentKind.Invoice)
            throw new Exception("Bill payments must be updated through bill allocations.");

        payment.PaymentCode = existing.PaymentCode;
        payment.DepositId = existing.DepositId;
        payment.PostingStatusId = await ApplySourceDocumentEditReconcileInvalidationAsync(existing.PostingStatusId, existing.OrganizationId, existing.OfficeId, payment.PaymentDate, default, currentUser, () => LoadJournalEntriesForPaymentDocumentAsync(existing.OrganizationId, existing));

        var revertPayment = existing;
        var revertAllocations = existing.LedgerLines
            .Select(line => new PaymentInvoiceAllocation
            {
                InvoiceId = line.InvoiceId,
                Amount = line.Amount,
                Description = line.Description ?? string.Empty
            })
            .ToList();

        try
        {
            if (existing.DepositId is not { } depositedId || depositedId == Guid.Empty)
            {
                await ClearPaymentDocumentLinksAsync(existing.OrganizationId, existing.PaymentId, currentUser);
                await DeleteJournalEntriesForPaymentAsync(existing);
            }

            var updatedPayment = await _accountingRepository.UpdatePaymentWithInvoiceAllocationsAsync(payment, allocations, currentUser);
            await CreateJournalEntriesFromInvoicePaymentDocumentAsync(updatedPayment.PaymentId, payment.OrganizationId, currentUser);
            await EnsurePaymentPostingStatusComplianceAsync(updatedPayment, currentUser);
            await ReconcileDepositSplitsForPaymentAsync(updatedPayment, currentUser);

            return await _accountingRepository.GetPaymentByIdAsync(updatedPayment.PaymentId, payment.OrganizationId)
                ?? updatedPayment;
        }
        catch
        {
            await TryRevertInvoicePaymentUpdateAsync(revertPayment, revertAllocations, currentUser);
            throw;
        }
    }

    private async Task TryRevertInvoicePaymentUpdateAsync(Payment revertPayment, IReadOnlyList<PaymentInvoiceAllocation> revertAllocations, Guid currentUser)
    {
        try
        {
            var current = await _accountingRepository.GetPaymentByIdAsync(revertPayment.PaymentId, revertPayment.OrganizationId);
            if (current == null)
                return;

            await ClearPaymentDocumentLinksAsync(current.OrganizationId, current.PaymentId, currentUser);
            await DeleteJournalEntriesForPaymentAsync(current);

            var restoredPayment = await _accountingRepository.UpdatePaymentWithInvoiceAllocationsAsync(revertPayment, revertAllocations, currentUser);
            await CreateJournalEntriesFromInvoicePaymentDocumentAsync(restoredPayment.PaymentId, revertPayment.OrganizationId, currentUser);
        }
        catch
        {
            // Best-effort revert after a failed invoice payment update.
        }
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

    private async Task SyncLinkedPaymentsFromInvoiceAsync(Invoice invoice, Invoice priorInvoice, Guid currentUser)
    {
        var paymentIds = invoice.LedgerLines
            .Where(line => line.PaymentId is { } paymentId && paymentId != Guid.Empty)
            .Select(line => line.PaymentId!.Value)
            .Distinct()
            .ToList();

        foreach (var paymentId in paymentIds)
            await SyncPaymentFromLinkedLedgerLinesAsync(paymentId, invoice, priorInvoice, currentUser);
    }

    private async Task SyncPaymentFromLinkedLedgerLinesAsync(Guid paymentId, Invoice invoice, Invoice priorInvoice, Guid currentUser)
    {
        if (paymentId == Guid.Empty)
            return;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, invoice.OrganizationId);
        if (payment == null)
            return;

        var linkedLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(paymentId, invoice.OrganizationId);
        if (linkedLines.Count == 0)
            return;

        var linkedTotal = linkedLines.Sum(line => line.Amount);
        var priorLinesById = priorInvoice.LedgerLines.ToDictionary(line => line.LedgerLineId);
        var changedMetadataLines = invoice.LedgerLines
            .Where(line => line.PaymentId == paymentId)
            .Where(line => !priorLinesById.TryGetValue(line.LedgerLineId, out var priorLine)
                || line.LedgerLineDate != priorLine.LedgerLineDate
                || line.CostCodeId != priorLine.CostCodeId
                || !string.Equals(line.Description, priorLine.Description, StringComparison.Ordinal))
            .ToList();

        if (changedMetadataLines.Select(line => (line.LedgerLineDate, line.CostCodeId)).Distinct().Count() > 1)
            throw new InvalidOperationException("Payment lines linked to the same Payment Document must use the same date and cost code.");

        var metadataSource = changedMetadataLines.FirstOrDefault();
        var changed = payment.Amount != linkedTotal;
        if (metadataSource != null)
        {
            changed = changed
                || payment.PaymentDate != metadataSource.LedgerLineDate
                || payment.CostCodeId != metadataSource.CostCodeId
                || (linkedLines.Count == 1 && !string.Equals(payment.Description, metadataSource.Description, StringComparison.Ordinal));
        }

        var lockContext = await TryLoadDepositedPaymentLockContextAsync(paymentId, invoice.OrganizationId);
        if (lockContext != null)
        {
            EnsureDepositedPaymentHeaderUnchanged(payment, lockContext, linkedTotal, metadataSource, invoice.InvoiceCode);
            return;
        }

        payment.Amount = linkedTotal;
        if (metadataSource != null)
        {
            payment.PaymentDate = metadataSource.LedgerLineDate;
            payment.CostCodeId = metadataSource.CostCodeId;
            if (linkedLines.Count == 1)
                payment.Description = metadataSource.Description;
        }

        if (!changed)
            return;

        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
        if (metadataSource != null)
            await SynchronizeInvoicePaymentLinesFromPaymentAsync(payment, currentUser);
    }

    private async Task SynchronizeInvoicePaymentLinesFromPaymentAsync(Payment payment, Guid currentUser)
    {
        var linkedLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(payment.PaymentId, payment.OrganizationId);
        if (linkedLines.Count == 0)
            return;

        if (linkedLines.Count > 1 && linkedLines.Sum(line => line.Amount) != payment.Amount)
            throw new InvalidOperationException("A multi-allocation Payment amount must be changed through its invoice allocations.");

        var synchronizeAmountAndDescription = linkedLines.Count == 1;
        var invoices = new List<Invoice>();
        foreach (var invoiceGroup in linkedLines.GroupBy(line => line.InvoiceId))
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceGroup.Key, payment.OrganizationId);
            if (invoice == null)
                continue;

            foreach (var paymentLine in invoiceGroup)
            {
                var line = invoice.LedgerLines.SingleOrDefault(candidate => candidate.LedgerLineId == paymentLine.LedgerLineId);
                if (line == null)
                    continue;

                if (synchronizeAmountAndDescription)
                {
                    invoice.PaidAmount += payment.Amount - line.Amount;
                    line.Amount = payment.Amount;
                    line.Description = payment.Description;
                }

                line.LedgerLineDate = payment.PaymentDate;
                line.CostCodeId = payment.CostCodeId;
                line.ModifiedBy = currentUser;
            }

            invoice.ModifiedBy = currentUser;
            invoices.Add(invoice);
        }

        if (invoices.Count > 0)
            await _accountingRepository.UpdateByIdsInTransactionAsync(invoices);
    }

    private static void EnsureInvoicePayment(Payment payment)
    {
        if (payment.PaymentKindId != (int)PaymentKind.Invoice)
            throw new ArgumentException("Invoice allocations are only supported for invoice payments.", nameof(payment));
    }
    #endregion
}

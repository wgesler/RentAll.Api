using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class AccountingManager : IAccountingManager
{
    const int PRORATE_DAYS = 30;
    Guid SystemOrganization = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IReservationRepository _reservationRepository;

    public AccountingManager(
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository,
        IAccountingRepository accountingRepository,
        IReservationRepository reservationRepository)
    {
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
        _accountingRepository = accountingRepository;
        _reservationRepository = reservationRepository;
    }

    #region Billing
    public async Task<List<LedgerLine>> CreateLedgerLinesForOrganizationIdAsync(Organization organization, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var lineItems = new List<LedgerLine>();
        var lineNumber = 1;

        var requestedDate = startDate.Date;
        var requestedMonth = startDate.Month;

        // Partial month = NO if: check-in is 1st, OR (month has 31 days AND check-in is 1st or 2nd)
        var daysInMonth = DateTime.DaysInMonth(requestedDate.Year, requestedDate.Month);
        var isMonthPartial = (requestedDate.Day != 1);
        var days = CalculateNumberOfBillingDays(startDate, endDate);


        // Get the Offices for the Organization
        IEnumerable<Office> offices = await _organizationRepository.GetAllAsync(organization.OrganizationId);

        foreach (var office in offices)
        {
            var officeLine = $"Office Base Fee ({office.Name}): ({startDate.LocalDateTime:MM/dd}-{endDate.LocalDateTime:MM/dd})";
            lineItems.Add(new LedgerLine { LineNumber = lineNumber++, Description = officeLine, Amount = organization.OfficeFee });

            var properties = await _propertyRepository.GetListByOfficeIdAsync(office.OrganizationId, Convert.ToString(office.OfficeId ));
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
        var costCodes = await _accountingRepository.GetAllByOfficeIdAsync(1, organizationId);
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
    #endregion

    #region Invoices
    public async Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId,
        string description, decimal amountPaid, Guid currentUser)
    {
        var invoices = new List<Invoice>();
        foreach (var invoiceGuid in invoiceGuids)
        {
            var invoice = await _accountingRepository.GetByIdAsync(invoiceGuid, organizationId);
            if (invoice == null) throw new Exception("Invalid Invoice");
            invoices.Add(invoice);
        }

        // Order invoices from the oldest to the newest
        invoices = invoices.Where(i => i.IsActive).OrderBy(i => i.InvoiceDate).ToList();

        var availableAmount = amountPaid;
        foreach (var invoice in invoices)
        {
            if (availableAmount <= 0)
                break;

            // Skip over already paid invoices
            var remainingBalance = invoice.TotalAmount - invoice.PaidAmount;
            if (remainingBalance <= 0)
                continue;



            if (availableAmount >= remainingBalance)
            {
                // Full payment for this invoice
                invoice.PaidAmount = invoice.TotalAmount;
                availableAmount -= remainingBalance;
                var maxLineNumber = invoice.LedgerLines.Any() ? invoice.LedgerLines.Max(ll => ll.LineNumber) : 0;
                invoice.LedgerLines.Add(new LedgerLine { InvoiceId = invoice.InvoiceId, LineNumber = maxLineNumber + 1, ReservationId = invoice.ReservationId, CostCodeId = costCodeId, Description = description, Amount = invoice.PaidAmount, CreatedBy = currentUser });
                await _accountingRepository.UpdateByIdAsync(invoice);
            }
            else
            {
                // Partial payment
                invoice.PaidAmount += availableAmount;
                availableAmount = 0;
                var maxLineNumber = invoice.LedgerLines.Any() ? invoice.LedgerLines.Max(ll => ll.LineNumber) : 0;
                invoice.LedgerLines.Add(new LedgerLine { InvoiceId = invoice.InvoiceId, LineNumber = maxLineNumber + 1, ReservationId = invoice.ReservationId, CostCodeId = costCodeId, Description = description, Amount = invoice.PaidAmount, CreatedBy = currentUser });
                await _accountingRepository.UpdateByIdAsync(invoice);
            }
        }

        var response = new InvoicePayment { Invoices = invoices, CreditRemaining = availableAmount };
        return response;
    }

    public async Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var ledgerLines = GetLedgerLinesByReservationIdAsync(reservation, startDate, endDate);
        await ApplyCostCodesAsync(reservation.OfficeId, reservation.OrganizationId, ledgerLines);

        return ledgerLines;
    }

    public List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var lineItems = new List<LedgerLine>();
        var lineNumber = 1;

        var requestedDate = startDate.Date;
        var requestedMonth = startDate.Month;

        var checkInDate = reservation.ArrivalDate.Date;
        var checkInMonth = reservation.ArrivalDate.Month;
        var checkOutDate = reservation.DepartureDate.Date;
        var checkOutMonth = reservation.DepartureDate.Month;

        // Determine if we have a partial month scenario
        // Partial month = NO if: check-in is 1st, OR (month has 31 days AND check-in is 1st or 2nd)
        var daysInCheckInMonth = DateTime.DaysInMonth(checkInDate.Year, checkInDate.Month);
        var isFirstMonthPartial = (checkInDate.Day != 1 && (daysInCheckInMonth == 31 && checkInDate.Day > 2));

        DateTime firstDayOfMonth = new DateTime(requestedDate.Year, requestedDate.Month, 1);
        DateTime lastDayOfMonth = new DateTime(requestedDate.Year, requestedDate.Month, DateTime.DaysInMonth(requestedDate.Year, requestedDate.Month));
        DateTime firstDayOfCheckInMonth = new DateTime(checkInDate.Year, checkInDate.Month, 1);
        DateTime lastDayOfCheckInMonth = new DateTime(checkInDate.Year, checkInDate.Month, DateTime.DaysInMonth(checkInDate.Year, checkInDate.Month));

        // Calculate secondMonth based on whether first month was prorated or not
        // If prorated: billed to end of first month, so second month starts the day after
        // If not prorated: billed for 30 days from check-in
        var secondMonthDate = (reservation.ProrateType == ProrateType.FirstMonth) ? lastDayOfCheckInMonth.AddDays(1) : checkInDate.AddDays(PRORATE_DAYS);
        var secondMonth = secondMonthDate.Month;

        var isFirstMonth = requestedMonth == checkInMonth;
        var isProratedMonth = (requestedMonth == checkInMonth && reservation.ProrateType == ProrateType.FirstMonth) ||
                              (requestedMonth == secondMonth && reservation.ProrateType == ProrateType.SecondMonth);

        // Get any first month lines

        // If you're in and out in the same month
        if (checkInMonth == requestedMonth && checkOutMonth == requestedMonth)
        {
            var days = CalculateNumberOfDays(checkInDate, checkOutDate, reservation.BillingType);
            AddRentalLine(days, reservation, checkInDate, checkOutDate, isProratedMonth, lineItems, ref lineNumber);
            GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
            AddMaidServiceLines(reservation, checkInDate, checkOutDate, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
            return lineItems;
        }

        // If this is your first month (only process special logic if partial month)
        if (requestedMonth == checkInMonth && isFirstMonthPartial)
        {
            var lastDay = isProratedMonth ? lastDayOfMonth : checkInDate.AddDays(PRORATE_DAYS - 1);
            var days = CalculateNumberOfDays(checkInDate, lastDay, reservation.BillingType);
            AddRentalLine(days, reservation, checkInDate, lastDay, isProratedMonth, lineItems, ref lineNumber);
            GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
            AddMaidServiceLines(reservation, checkInDate, lastDay, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, checkInDate, lastDay, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
            return lineItems;
        }

        // If this is your second month (only process special logic if partial month)
        if (requestedMonth == secondMonth && isFirstMonthPartial)
        {
            var firstDay = isProratedMonth ? checkInDate.AddDays(PRORATE_DAYS) : firstDayOfMonth;
            var days = CalculateNumberOfDays(firstDay, lastDayOfMonth, reservation.BillingType);
            AddRentalLine(days, reservation, firstDay, lastDayOfMonth, isProratedMonth, lineItems, ref lineNumber);
            AddMaidServiceLines(reservation, firstDay, lastDayOfMonth, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
            foreach (var extraFeeLine in reservation.ExtraFeeLines)
                AddExtraFeeLines(extraFeeLine, firstDay, lastDayOfMonth, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
            return lineItems;
        }

        // Otherwise, simply bill for the entire month
        var checkoutDays = CalculateNumberOfDays(firstDayOfMonth, lastDayOfMonth, reservation.BillingType);
        AddRentalLine(checkoutDays, reservation, firstDayOfMonth, lastDayOfMonth, isProratedMonth, lineItems, ref lineNumber);
        GetFirstMonthLines(reservation, isFirstMonth, lineItems, ref lineNumber);
        AddMaidServiceLines(reservation, firstDayOfMonth, lastDayOfMonth, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
            AddExtraFeeLines(extraFeeLine, firstDayOfMonth, lastDayOfMonth, requestedDate.Year, requestedMonth, lineItems, ref lineNumber);
        return lineItems;
    }

    public async Task ApplyCostCodesAsync(int officeId, Guid organizationId, List<LedgerLine> ledgerLines)
    {
        var costCodes = await _accountingRepository.GetAllByOfficeIdAsync(officeId, organizationId);
        foreach (var line in ledgerLines)
        {
            var costCode = null as CostCode;
            switch (line.Description)
            {
                case string desc when desc.StartsWith("Security Deposit Waiver", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Security Deposit Waiver"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
                case string desc when desc.StartsWith("Deposit", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Deposit"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
                case string desc when desc.StartsWith("Rental Fee", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Rent Property"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
                case string desc when desc.StartsWith("Maid Service", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Maid Service"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
                case string desc when desc.StartsWith("Pet Fee", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Pet Fee"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
                case string desc when desc.StartsWith("Departure Fee", StringComparison.OrdinalIgnoreCase):
                    costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Departure Fee"));
                    if (costCode != null) line.CostCodeId = costCode.CostCodeId;
                    continue;
            }
        }
    }
    #endregion

    #region Private Methods
    private void GetFirstMonthLines(Reservation reservation, bool isFirstMonth, List<LedgerLine> lines, ref int lineNumber)
    {
        if (!isFirstMonth)
            return;

        if (reservation.DepositType == DepositType.Deposit)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Deposit", Amount = reservation.Deposit });
        if (reservation.HasPets)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Pet Fee", Amount = reservation.PetFee });
        if (reservation.DepartureFee >= 0)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Departure Fee", Amount = reservation.DepartureFee });

        // We add the one-time fees up front
        foreach (var extraFeeLine in reservation.ExtraFeeLines)
        {
            if (extraFeeLine.FeeFrequency == FrequencyType.OneTime)
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"{extraFeeLine.FeeDescription}", Amount = extraFeeLine.FeeAmount, CostCodeId = extraFeeLine.CostCodeId });
        }
    }

    private void AddRentalLine(int days, Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate, bool isProratedMonth, List<LedgerLine> lines, ref int lineNumber)
    {
        var rentLine = $"Rental Fee ({startDate.LocalDateTime:MM/dd}-{endDate.LocalDateTime:MM/dd})";
        if (reservation.BillingType == BillingType.Monthly)
        {
            if (isProratedMonth)
            {
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = rentLine, Amount = (reservation.BillingRate / PRORATE_DAYS) * days });
                if (reservation.DepositType == DepositType.SDW)
                    lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit Waiver", Amount = (reservation.Deposit / PRORATE_DAYS) * days });
            }
            else
            {
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = rentLine, Amount = reservation.BillingRate });
                if (reservation.DepositType == DepositType.SDW)
                    lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit Waiver", Amount = reservation.Deposit });
            }
        }
        else
        {
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = rentLine, Amount = days * reservation.BillingRate });
            if (reservation.DepositType == DepositType.SDW)
                lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = "Security Deposit Waiver", Amount = (reservation.Deposit / PRORATE_DAYS) * days });
        }


    }

    private void AddMaidServiceLines(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate, int requestedYear, int requestedMonth, List<LedgerLine> lines, ref int lineNumber)
    {
        var sDate = reservation.MaidStartDate > startDate ? reservation.MaidStartDate : startDate;
        var dDate = endDate > reservation.DepartureDate.AddDays(-7) ? reservation.DepartureDate : endDate;

        int maidServices = 0;
        switch (reservation.Frequency)
        {
            case FrequencyType.Weekly:
                maidServices = CountNumberOfWeekDaysInMonth(reservation.MaidStartDate, sDate, dDate, requestedYear, requestedMonth);
                break;
            case FrequencyType.EOW:
                maidServices = CountEowDaysInMonth(reservation.MaidStartDate, sDate, dDate, requestedYear, requestedMonth);
                break;
            default:
                maidServices = CountNumberOfMonths(reservation.MaidStartDate, sDate, dDate, requestedYear, requestedMonth, reservation.Frequency);
                break;
        }

        if (maidServices > 0)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"Maid Service ({maidServices} times)", Amount = maidServices * reservation.MaidServiceFee });
    }

    private void AddExtraFeeLines(ExtraFeeLine extraFeeLine, DateTimeOffset startDate, DateTimeOffset endDate, int requestedYear, int requestedMonth, List<LedgerLine> lines, ref int lineNumber)
    {
        int fees = 0;
        switch (extraFeeLine.FeeFrequency)
        {
            case FrequencyType.Weekly:
                fees = CountNumberOfWeekDaysInMonth(startDate, startDate, endDate, requestedYear, requestedMonth);
                break;
            case FrequencyType.EOW:
                fees = CountEowDaysInMonth(startDate, startDate, endDate, requestedYear, requestedMonth);
                break;
            default:
                fees = CountNumberOfMonths(startDate, startDate, endDate, requestedYear, requestedMonth, extraFeeLine.FeeFrequency);
                break;
        }

        if (fees > 0)
            lines.Add(new LedgerLine { LineNumber = lineNumber++, Description = $"{extraFeeLine.FeeDescription} ({fees} times)", Amount = fees * extraFeeLine.FeeAmount, CostCodeId = extraFeeLine.CostCodeId });
    }

    #endregion

    #region Day Calculation Methods
    private static int CalculateNumberOfBillingDays(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;
        if (end < start) return 0;

        var days = (end - start).Days;
        return days;
    }

    private static int CalculateNumberOfDays(DateTimeOffset startDate, DateTimeOffset endDate, BillingType billingType)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;
        if (end < start) return 0;

        var days = (end - start).Days;
        if (billingType != BillingType.Nightly)
            days++;
        return days;
    }

    private static int CountNumberOfWeekDaysInMonth(DateTimeOffset maidStartDate, DateTimeOffset sDate, DateTimeOffset dDate, int currentYear, int currentMonth)
    {
        int count = 0;
        for (DateTimeOffset d = maidStartDate; d <= dDate; d = d.AddDays(7))
        {
            if (d >= sDate && d <= dDate)
                count++;
        }

        return count;
    }

    private static int CountEowDaysInMonth(DateTimeOffset maidStartDate, DateTimeOffset sDate, DateTimeOffset dDate, int currentYear, int currentMonth)
    {
        int count = 0;
        for (DateTimeOffset d = maidStartDate; d <= dDate; d = d.AddDays(14))
        {
            if (d >= sDate && d <= dDate)
                count++;
        }

        return count;
    }

    private static int CountNumberOfMonths(DateTimeOffset maidStartDate, DateTimeOffset sDate, DateTimeOffset dDate, int currentYear, int currentMonth, FrequencyType frequency)
    {
        // Determine the interval based on frequency
        int monthInterval;
        switch (frequency)
        {
            case FrequencyType.Monthly:
                monthInterval = 1;
                break;
            case FrequencyType.Quarterly:
                monthInterval = 3;
                break;
            case FrequencyType.BiAnnually:
                monthInterval = 6;
                break;
            case FrequencyType.Annually:
                monthInterval = 12;
                break;
            default:
                return 0;
        }

        int count = 0;
        for (DateTimeOffset d = maidStartDate; d <= dDate; d = d.AddMonths(monthInterval))
        {
            if (d >= sDate && d <= dDate)
                count++;
        }

        return count;
    }
    #endregion
}

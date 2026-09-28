using System.Globalization;
using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain;

/// <summary>
/// Day counts for invoice rental lines — same rules as Get Charges
/// (<see cref="Managers.AccountingManager.GetLedgerLinesByReservationIdAsync"/> / <c>CalculateNumberOfDays</c>).
/// </summary>
public static class InvoiceBillingDays
{
    private static readonly Regex RentalFeePeriodRegex = new(
        @"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static int CalculateNumberOfDays(
        DateOnly startDate,
        DateOnly endDate,
        BillingType billingType,
        bool isDepartureMonthYear,
        bool isLastDayOfMonth)
    {
        if (endDate < startDate)
            return 0;
        if (endDate == startDate)
            return billingType == BillingType.Nightly && isDepartureMonthYear ? 0 : 1;

        var days = endDate.DayNumber - startDate.DayNumber;
        if (billingType != BillingType.Nightly
            || (billingType == BillingType.Nightly && !isDepartureMonthYear && isLastDayOfMonth))
        {
            days++;
        }

        return days;
    }

    public static bool IsDepartureMonthYear(DateOnly invoicePeriodEnd, DateOnly billingDepartureDate)
        => invoicePeriodEnd.Month == billingDepartureDate.Month
           && invoicePeriodEnd.Year == billingDepartureDate.Year;

    public static bool IsLastDayOfMonth(DateOnly date)
        => date.Day == DateTime.DaysInMonth(date.Year, date.Month);

    public static (DateOnly Start, DateOnly End) ResolveInvoiceBillingPeriod(Invoice invoice)
    {
        if (TryParseInvoicePeriodRange(invoice.InvoicePeriod, out var parsedStart, out var parsedEnd))
            return (parsedStart, parsedEnd);

        var monthStart = invoice.AccountingPeriod != default
            ? new DateOnly(invoice.AccountingPeriod.Year, invoice.AccountingPeriod.Month, 1)
            : new DateOnly(invoice.InvoiceDate.Year, invoice.InvoiceDate.Month, 1);

        return (monthStart, ReservationStayDays.LastDayOfMonth(monthStart));
    }

    public static int SumRentalFeeDaysFromLedgerLines(
        IReadOnlyList<LedgerLine> lines,
        Reservation reservation,
        DateOnly invoicePeriodStart,
        DateOnly invoicePeriodEnd,
        DateOnly billingDepartureDate)
    {
        var billingType = reservation.BillingType;
        var isDepartureMonthYear = IsDepartureMonthYear(invoicePeriodEnd, billingDepartureDate);
        var isLastDayOfMonth = IsLastDayOfMonth(invoicePeriodEnd);
        var total = 0;

        foreach (var line in lines)
        {
            if (line.Amount == 0)
                continue;

            total += CountRentalFeeDescriptionDays(
                line.Description,
                invoicePeriodEnd.Year,
                billingType,
                isDepartureMonthYear,
                isLastDayOfMonth,
                billingDepartureDate,
                invoicePeriodEnd);
        }

        return total;
    }

    public static int CountRentalFeeDescriptionDays(
        string? description,
        int referenceYear,
        BillingType billingType,
        bool isDepartureMonthYear,
        bool isLastDayOfMonth,
        DateOnly billingDepartureDate,
        DateOnly invoicePeriodEnd)
    {
        if (!TryParseRentalFeePeriod(description, referenceYear, out var periodStart, out var periodEnd))
            return 0;

        var countEnd = RestoreRentalPeriodEndForGetChargesCount(
            periodEnd,
            billingType,
            isDepartureMonthYear,
            billingDepartureDate);

        return CalculateNumberOfDays(periodStart, countEnd, billingType, isDepartureMonthYear, isLastDayOfMonth);
    }

    /// <summary>
    /// Nightly departure-month lines store end date one day early in the description (see AddRentalLine).
    /// </summary>
    public static DateOnly RestoreRentalPeriodEndForGetChargesCount(
        DateOnly parsedPeriodEnd,
        BillingType billingType,
        bool isDepartureMonthYear,
        DateOnly billingDepartureDate)
    {
        if (billingType != BillingType.Nightly || !isDepartureMonthYear)
            return parsedPeriodEnd;

        if (parsedPeriodEnd >= billingDepartureDate)
            return parsedPeriodEnd;

        var restored = parsedPeriodEnd.AddDays(1);
        if (restored == billingDepartureDate
            && parsedPeriodEnd.Year == billingDepartureDate.Year
            && parsedPeriodEnd.Month == billingDepartureDate.Month)
        {
            return restored;
        }

        return parsedPeriodEnd;
    }

    public static bool TryParseRentalFeePeriod(string? description, int referenceYear, out DateOnly periodStart, out DateOnly periodEnd)
    {
        periodStart = default;
        periodEnd = default;

        if (string.IsNullOrWhiteSpace(description))
            return false;

        var match = RentalFeePeriodRegex.Match(description.Trim());
        if (!match.Success)
            return false;

        if (!TryParseMonthDay(match.Groups["start"].Value, referenceYear, out periodStart))
            return false;

        if (!TryParseMonthDay(match.Groups["end"].Value, referenceYear, out periodEnd))
            return false;

        if (periodEnd < periodStart)
            periodEnd = periodEnd.AddYears(1);

        return true;
    }

    public static bool TryParseInvoicePeriodRange(string? invoicePeriod, out DateOnly start, out DateOnly end)
    {
        start = default;
        end = default;
        if (string.IsNullOrWhiteSpace(invoicePeriod))
            return false;

        var parts = invoicePeriod.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        if (!DateOnly.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
            return false;

        if (!DateOnly.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out end))
            return false;

        return end >= start;
    }

    private static bool TryParseMonthDay(string monthDay, int year, out DateOnly date)
    {
        date = default;
        var parts = monthDay.Split('/');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var day))
            return false;

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}

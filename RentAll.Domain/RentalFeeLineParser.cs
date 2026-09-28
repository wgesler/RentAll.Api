using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain;

public static class RentalFeeLineParser
{
    private static readonly Regex RentalFeePeriodRegex = new(
        @"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParseRentalFeePeriod(string? description, int referenceYear, out DateOnly periodStart, out DateOnly periodEnd)
        => InvoiceBillingDays.TryParseRentalFeePeriod(description, referenceYear, out periodStart, out periodEnd);

    public static IReadOnlyList<Invoice> FilterInvoicesOnOrAfterAccountingStart(
        IReadOnlyList<Invoice> invoices,
        DateOnly invoiceStartDate)
    {
        return invoices
            .Where(invoice => InvoiceOnOrAfterInvoiceStart(invoice, invoiceStartDate))
            .ToList();
    }

    public static IReadOnlyList<string> CollectRentalFeeLineDescriptions(
        IEnumerable<Invoice> invoices,
        DateOnly? invoiceStartDate = null)
    {
        var scopedInvoices = invoiceStartDate.HasValue
            ? FilterInvoicesOnOrAfterAccountingStart(invoices.ToList(), invoiceStartDate.Value)
            : invoices;

        var descriptions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var invoice in scopedInvoices)
        {
            var referenceYear = invoice.AccountingPeriod != default
                ? invoice.AccountingPeriod.Year
                : invoice.InvoiceDate.Year;

            foreach (var line in invoice.LedgerLines ?? [])
            {
                if (line.Amount == 0)
                    continue;

                var description = (line.Description ?? string.Empty).Trim();
                if (!TryParseRentalFeePeriod(description, referenceYear, out var periodStart, out var periodEnd))
                    continue;

                if (invoiceStartDate.HasValue
                    && !ClipRangeToInvoiceStart(periodStart, periodEnd, invoiceStartDate.Value).HasValue)
                {
                    continue;
                }

                if (seen.Add(description))
                    descriptions.Add(description);
            }
        }

        descriptions.Sort(StringComparer.OrdinalIgnoreCase);
        return descriptions;
    }

    public static int CalculateDaysBilledFromInvoices(
        IReadOnlyList<Invoice> invoices,
        Reservation reservation,
        DateOnly invoiceStartDate,
        DateOnly? billingStayEndDate = null)
    {
        var scopedInvoices = FilterInvoicesOnOrAfterAccountingStart(invoices, invoiceStartDate);
        var billingDeparture = billingStayEndDate ?? reservation.DepartureDate;
        var billingType = reservation.BillingType;
        var ranges = new List<(DateOnly Start, DateOnly End)>();

        foreach (var invoice in scopedInvoices)
        {
            var (_, invoicePeriodEnd) = InvoiceBillingDays.ResolveInvoiceBillingPeriod(invoice);
            var isDepartureMonthYear = InvoiceBillingDays.IsDepartureMonthYear(invoicePeriodEnd, billingDeparture);
            var referenceYear = invoice.AccountingPeriod != default
                ? invoice.AccountingPeriod.Year
                : invoice.InvoiceDate.Year;

            foreach (var line in invoice.LedgerLines ?? [])
            {
                if (line.Amount == 0)
                    continue;

                if (!TryParseRentalFeePeriod(line.Description, referenceYear, out var periodStart, out var periodEnd))
                    continue;

                var clipped = ClipRangeToInvoiceStart(periodStart, periodEnd, invoiceStartDate);
                if (!clipped.HasValue)
                    continue;

                (periodStart, periodEnd) = clipped.Value;

                var countEnd = InvoiceBillingDays.RestoreRentalPeriodEndForGetChargesCount(
                    periodEnd,
                    billingType,
                    isDepartureMonthYear,
                    billingDeparture);

                ranges.Add((periodStart, countEnd));
            }
        }

        return SumGetChargesDaysForMergedRanges(ranges, billingType, billingDeparture);
    }

    private static int SumGetChargesDaysForMergedRanges(
        IReadOnlyList<(DateOnly Start, DateOnly End)> ranges,
        BillingType billingType,
        DateOnly billingDepartureDate)
    {
        if (ranges.Count == 0)
            return 0;

        var merged = MergeDateRanges(ranges);
        var total = 0;

        foreach (var range in merged)
        {
            var isDepartureMonthYear = InvoiceBillingDays.IsDepartureMonthYear(range.End, billingDepartureDate);
            var isLastDayOfMonth = InvoiceBillingDays.IsLastDayOfMonth(range.End);
            total += InvoiceBillingDays.CalculateNumberOfDays(
                range.Start,
                range.End,
                billingType,
                isDepartureMonthYear,
                isLastDayOfMonth);
        }

        return total;
    }

    private static List<(DateOnly Start, DateOnly End)> MergeDateRanges(IReadOnlyList<(DateOnly Start, DateOnly End)> ranges)
    {
        var sorted = ranges.OrderBy(range => range.Start).ToList();
        var merged = new List<(DateOnly Start, DateOnly End)>();

        foreach (var range in sorted)
        {
            if (merged.Count == 0 || range.Start > merged[^1].End.AddDays(1))
            {
                merged.Add(range);
                continue;
            }

            var current = merged[^1];
            if (range.End > current.End)
                merged[^1] = (current.Start, range.End);
        }

        return merged;
    }

    private static bool InvoiceOnOrAfterInvoiceStart(Invoice invoice, DateOnly invoiceStartDate)
    {
        var invoiceMonthStart = invoice.AccountingPeriod != default
            ? new DateOnly(invoice.AccountingPeriod.Year, invoice.AccountingPeriod.Month, 1)
            : new DateOnly(invoice.InvoiceDate.Year, invoice.InvoiceDate.Month, 1);

        var invoiceMonthEnd = ReservationStayDays.LastDayOfMonth(invoiceMonthStart);
        return invoiceMonthEnd >= invoiceStartDate;
    }

    private static (DateOnly Start, DateOnly End)? ClipRangeToInvoiceStart(
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly invoiceStartDate)
    {
        if (periodEnd < invoiceStartDate)
            return null;

        var clippedStart = periodStart < invoiceStartDate ? invoiceStartDate : periodStart;
        return (clippedStart, periodEnd);
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

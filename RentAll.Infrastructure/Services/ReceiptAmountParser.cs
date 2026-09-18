using System.Globalization;
using System.Text.RegularExpressions;

namespace RentAll.Infrastructure.Services;

public static class ReceiptAmountParser
{
    private static readonly Regex GrandTotalLabelPattern = new(@"\bgrand\s+total\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TotalLabelPattern = new(@"\btotal\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExcludedTotalLabelPattern = new(
        @"\b(?:sub\s*total|total\s*tax|tax(?:es)?\s*total|items?\s+total|merchandise\s+total|taxable)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CurrencyAmountPattern = new(
        @"\$?\s*(\d{1,3}(?:,\d{3})*\.\d{2}|\d+\.\d{2})\b",
        RegexOptions.Compiled);
    private static readonly Regex AmountOnlyLinePattern = new(
        @"^\$?\s*\d{1,3}(?:,\d{3})*\.\d{2}\s*$",
        RegexOptions.Compiled);

    public static decimal? ResolveAmount(decimal? total, decimal? subtotal, decimal? tax, string? fullContent)
    {
        var labeled = ReadLabeledTotal(fullContent);
        if (labeled.HasValue)
            return labeled.Value;

        if (total.HasValue && subtotal.HasValue && tax.HasValue && tax.Value != 0 && total.Value == subtotal.Value)
            return decimal.Round(subtotal.Value + tax.Value, 2, MidpointRounding.AwayFromZero);

        if (total.HasValue)
            return total.Value;

        if (subtotal.HasValue && tax.HasValue)
            return decimal.Round(subtotal.Value + tax.Value, 2, MidpointRounding.AwayFromZero);

        return subtotal;
    }

    public static decimal? ReadLabeledTotal(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        decimal? grandTotal = null;
        decimal? total = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (GrandTotalLabelPattern.IsMatch(line))
            {
                var amount = ReadAmountOnLineOrNext(lines, index);
                if (amount.HasValue)
                    grandTotal = amount;
                continue;
            }

            if (ExcludedTotalLabelPattern.IsMatch(line) || !TotalLabelPattern.IsMatch(line))
                continue;

            var labeledTotal = ReadAmountOnLineOrNext(lines, index);
            if (labeledTotal.HasValue)
                total = labeledTotal;
        }

        return grandTotal ?? total;
    }

    private static decimal? ReadAmountOnLineOrNext(string[] lines, int index)
    {
        var onLine = ReadLastCurrencyAmount(lines[index]);
        if (onLine.HasValue)
            return onLine;

        if (index + 1 >= lines.Length)
            return null;

        var nextLine = lines[index + 1];
        return AmountOnlyLinePattern.IsMatch(nextLine) ? ReadLastCurrencyAmount(nextLine) : null;
    }

    private static decimal? ReadLastCurrencyAmount(string line)
    {
        Match? lastMatch = null;
        foreach (Match match in CurrencyAmountPattern.Matches(line))
            lastMatch = match;

        if (lastMatch == null)
            return null;

        if (!decimal.TryParse(lastMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return null;

        return amount;
    }
}

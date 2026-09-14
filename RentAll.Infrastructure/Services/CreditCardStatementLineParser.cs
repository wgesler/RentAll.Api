using RentAll.Domain.Enums;
using RentAll.Domain.Models.Maintenances;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RentAll.Infrastructure.Services;

public static class CreditCardStatementLineParser
{
    private static readonly Regex CardLastFourPattern = new(
        @"(?:[#*xX\u2022\u00B7\s.-]{4,20})(\d{4})\b"
        + @"|(?:ending|ends)\s+(?:in|with)\s+[#*xX]*(\d{4})"
        + @"|\b(?:visa|master\s*card|mc|discover|disc|amex|american\s*express)\b[^\d]{0,40}(?:[#*xX\s.-]{0,20})(\d{4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SummaryRowPattern = new(
        @"^(?:previous|opening|new|ending|current)\s+balance\b|^minimum\s+payment\b|^total\b|^subtotal\b|^interest\s+charged\b|^fees?\s+charged\b|^credit\s+limit\b|^available\s+credit\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PaymentRowPattern = new(
        @"\b(?:payment|autopay|auto\s*pay|thank\s*you|pymt|online\s+pmt|mobile\s+payment|payment\s+received|bill\s*pay)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(
        @"\+?1?[\s.-]?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}\b|\b\d{3}[\s.-]\d{4}\b",
        RegexOptions.Compiled);
    private static readonly Regex AddressPattern = new(
        @"\b\d+\s+[\w.#-]+(?:\s+[\w.#-]+){0,4}\s+(?:st|street|ave|avenue|rd|road|blvd|boulevard|dr|drive|ln|lane|way|ct|court|hwy|highway|pkwy|parkway|cir|circle|pl|place|ste|suite|apt|unit)\b.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingLocationPattern = new(
        @"\s+(?:[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+){0,2}\s+)?[A-Z]{2}(?:\s+\d{5}(?:-\d{4})?)?\s*$",
        RegexOptions.Compiled);
    private static readonly Regex TrailingNumberedLocationPattern = new(
        @"\s+\d+(?:[A-Za-z][A-Za-z0-9]*|(?:\s+[A-Za-z].*))\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ZipPattern = new(@"\b\d{5}(?:-\d{4})?\b", RegexOptions.Compiled);

    public static CreditCardStatementExtraction ParseTables(IReadOnlyList<IReadOnlyList<string>> tables, string fullText)
    {
        var warnings = new List<string>();
        var statementCardLastFour = ExtractCardLastFour(fullText);
        var statementCardTypeId = ParseCardType(fullText);
        var lines = new List<CreditCardStatementLine>();

        foreach (var table in tables)
        {
            if (table.Count == 0)
                continue;

            var parsed = ParseTable(table, statementCardLastFour, statementCardTypeId);
            foreach (var line in parsed)
                lines.Add(line);
        }

        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(fullText))
        {
            foreach (var line in ParsePlainTextLines(fullText, statementCardLastFour, statementCardTypeId))
                lines.Add(line);
        }

        if (lines.Count == 0)
            warnings.Add("No credit card charges were detected in the uploaded file.");

        return new CreditCardStatementExtraction
        {
            StatementCardLastFour = statementCardLastFour,
            StatementCardTypeId = statementCardTypeId,
            FullText = fullText ?? string.Empty,
            Lines = lines,
            Warnings = warnings
        };
    }

    public static string? ExtractCardLastFour(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = CardLastFourPattern.Match(text);
        if (!match.Success)
            return null;

        return match.Groups.Cast<Group>()
            .Skip(1)
            .Select(group => group.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();
    }

    public static int? ParseCardType(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (Regex.IsMatch(text, @"\bvisa\b", RegexOptions.IgnoreCase))
            return (int)CardType.Visa;

        if (Regex.IsMatch(text, @"\b(?:master\s*card|mastercard|\bmc\b)\b", RegexOptions.IgnoreCase))
            return (int)CardType.MasterCard;

        if (Regex.IsMatch(text, @"\b(?:discover|\bdisc\b)\b", RegexOptions.IgnoreCase))
            return (int)CardType.Discover;

        if (Regex.IsMatch(text, @"\b(?:amex|american\s*express)\b", RegexOptions.IgnoreCase))
            return (int)CardType.AmericanExpress;

        return null;
    }

    private static List<CreditCardStatementLine> ParseTable(IReadOnlyList<string> rows, string? statementCardLastFour, int? statementCardTypeId)
    {
        var grid = rows.Select(SplitRow).Where(cells => cells.Count > 0).ToList();
        if (grid.Count == 0)
            return [];

        var headerIndex = grid.FindIndex(IsHeaderRow);
        var header = headerIndex >= 0 ? grid[headerIndex] : new List<string>();
        var dateCol = FindColumn(header, "date", "trans date", "transaction date", "post date", "posted", "charge date");
        var vendorCol = FindColumn(header, "merchant", "vendor", "description", "payee", "name");
        var amountCol = FindColumn(header, "amount", "charge", "debit", "credit", "usd");
        var cardCol = FindColumn(header, "card", "last 4", "last4", "account");

        var startRow = headerIndex >= 0 ? headerIndex + 1 : 0;
        var lines = new List<CreditCardStatementLine>();
        for (var i = startRow; i < grid.Count; i++)
        {
            var cells = grid[i];
            var line = BuildLine(cells, dateCol, vendorCol, amountCol, cardCol, statementCardLastFour, statementCardTypeId);
            if (line != null)
                lines.Add(line);
        }

        return lines;
    }

    private static CreditCardStatementLine? BuildLine(IReadOnlyList<string> cells, int dateCol, int vendorCol, int amountCol, int cardCol, string? statementCardLastFour, int? statementCardTypeId)
    {
        var date = dateCol >= 0 ? ParseDate(GetCell(cells, dateCol)) : FindDate(cells);
        var amount = amountCol >= 0 ? ParseAmount(GetCell(cells, amountCol)) : FindAmount(cells);
        var vendor = CleanVendorName(vendorCol >= 0 ? CleanText(GetCell(cells, vendorCol)) : FindVendor(cells, date, amount));
        var cardLastFour = cardCol >= 0 ? ExtractCardLastFour(GetCell(cells, cardCol)) : ExtractCardLastFour(string.Join(" ", cells));
        var rowText = CleanText(string.Join(" ", cells.Where(cell => !string.IsNullOrWhiteSpace(cell))));

        if (IsSummaryRow(vendor) || IsSummaryRow(rowText) || IsPaymentRow(vendor, rowText, amount))
            return null;

        if (!date.HasValue || !amount.HasValue || amount.Value == 0 || string.IsNullOrWhiteSpace(vendor))
            return null;

        return new CreditCardStatementLine
        {
            ChargeDate = date,
            Amount = decimal.Round(amount.Value, 2, MidpointRounding.AwayFromZero),
            VendorName = vendor,
            Description = null,
            CardLastFour = cardLastFour ?? statementCardLastFour,
            CardTypeId = statementCardTypeId
        };
    }

    private static List<CreditCardStatementLine> ParsePlainTextLines(string fullText, string? statementCardLastFour, int? statementCardTypeId)
    {
        var lines = new List<CreditCardStatementLine>();
        foreach (var raw in fullText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var cells = Regex.Split(raw, @"\s{2,}").Where(cell => !string.IsNullOrWhiteSpace(cell)).ToList();
            if (cells.Count < 2)
                cells = raw.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var line = BuildLine(cells, -1, -1, -1, -1, statementCardLastFour, statementCardTypeId);
            if (line != null)
                lines.Add(line);
        }

        return lines;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> cells)
    {
        var joined = string.Join(" ", cells).ToLowerInvariant();
        return joined.Contains("date") && (joined.Contains("amount") || joined.Contains("merchant") || joined.Contains("description") || joined.Contains("vendor"));
    }

    private static bool IsSummaryRow(string? text) => !string.IsNullOrWhiteSpace(text) && SummaryRowPattern.IsMatch(text.Trim());

    private static bool IsPaymentRow(string? vendor, string? description, decimal? amount)
    {
        if (amount is < 0)
            return true;

        var text = string.Join(" ", new[] { vendor, description }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return !string.IsNullOrWhiteSpace(text) && PaymentRowPattern.IsMatch(text);
    }

    private static string? CleanVendorName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var vendor = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? value;
        var cutIndex = vendor.IndexOfAny(['#', '*']);
        if (cutIndex >= 0)
            vendor = vendor[..cutIndex];
        vendor = PhonePattern.Replace(vendor, " ");
        vendor = AddressPattern.Replace(vendor, " ");
        vendor = ZipPattern.Replace(vendor, " ");
        vendor = TrailingNumberedLocationPattern.Replace(vendor.Trim(), string.Empty);
        vendor = TrailingLocationPattern.Replace(vendor.Trim(), string.Empty);
        vendor = Regex.Replace(vendor, @"\s+", " ").Trim(' ', ',', '-', '/', '|');
        return vendor.Length == 0 ? null : vendor;
    }

    private static int FindColumn(IReadOnlyList<string> header, params string[] names)
    {
        for (var i = 0; i < header.Count; i++)
        {
            var value = (header[i] ?? string.Empty).Trim().ToLowerInvariant();
            if (names.Any(name => value.Contains(name)))
                return i;
        }

        return -1;
    }

    private static DateOnly? FindDate(IReadOnlyList<string> cells) => cells.Select(ParseDate).FirstOrDefault(date => date.HasValue);

    private static decimal? FindAmount(IReadOnlyList<string> cells)
    {
        for (var i = cells.Count - 1; i >= 0; i--)
        {
            var amount = ParseAmount(cells[i]);
            if (amount.HasValue)
                return amount;
        }

        return null;
    }

    private static string? FindVendor(IReadOnlyList<string> cells, DateOnly? date, decimal? amount)
    {
        foreach (var cell in cells)
        {
            if (string.IsNullOrWhiteSpace(cell) || ParseDate(cell).HasValue || ParseAmount(cell).HasValue)
                continue;

            var cleaned = CleanText(cell);
            if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Any(char.IsLetter))
                return cleaned;
        }

        return date.HasValue && amount.HasValue ? null : CleanText(cells.FirstOrDefault(cell => !string.IsNullOrWhiteSpace(cell)));
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (DateOnly.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var current))
            return current;

        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant))
            return invariant;

        string[] formats = ["M/d/yyyy", "M/d/yy", "MM/dd/yyyy", "MM/dd/yy", "yyyy-MM-dd", "MMM d, yyyy", "MMM dd yyyy", "d-MMM-yyyy"];
        if (DateOnly.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;

        return null;
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        var isCredit = trimmed.Contains('(') && trimmed.Contains(')') || trimmed.EndsWith("CR", StringComparison.OrdinalIgnoreCase);
        var digits = Regex.Replace(trimmed, @"[^\d.\-]", string.Empty);
        if (!decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed == 0)
            return null;

        return isCredit ? -Math.Abs(parsed) : parsed;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = Regex.Replace(value.Trim(), @"\s+", " ");
        return cleaned.Length == 0 ? null : cleaned;
    }

    private static string GetCell(IReadOnlyList<string> cells, int index) => index >= 0 && index < cells.Count ? cells[index] : string.Empty;

    private static List<string> SplitRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
            return [];

        if (row.Contains('\t'))
            return row.Split('\t').Select(cell => cell.Trim()).ToList();

        return row.Split(['|'], StringSplitOptions.None).Select(cell => cell.Trim()).ToList();
    }
}

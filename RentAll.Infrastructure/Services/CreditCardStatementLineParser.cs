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
        @"\b(?:payment|autopay|auto\s*pay|thank\s*you|pymt|online\s+pmt|mobile\s+payment|payment\s+received|bill\s*pay|refund|reversal)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(
        @"\+?1?[\s.-]?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}\b|\b\d{3}[\s.-]\d{4}\b",
        RegexOptions.Compiled);
    private static readonly Regex AddressPattern = new(
        @"\b\d+\s+[\w.#-]+(?:\s+[\w.#-]+){0,4}\s+(?:st|street|ave|avenue|rd|road|blvd|boulevard|dr|drive|ln|lane|way|ct|court|hwy|highway|pkwy|parkway|cir|circle|pl|place|ste|suite|apt|unit)\b.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const string UsStatePattern = @"A[LKZR]|C[AOT]|D[CE]|F[LM]|GA|HI|I[DLNA]|K[SY]|LA|M[EDAINSOTP]|N[EVHJMYCD]|O[HKR]|P[AR]|RI|S[CD]|T[NX]|UT|V[AT]|W[AIVY]";
    private static readonly Regex TrailingCityStatePattern = new(
        $@"\s+[A-Za-z]+,?\s+\b(?:{UsStatePattern})\b\.?(?:\s+\d{{5}}(?:-\d{{4}})?)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingStateOnlyPattern = new(
        $@"[,\s]+\b(?:{UsStatePattern})\b\.?(?:\s+\d{{5}}(?:-\d{{4}})?)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingNumberedLocationPattern = new(
        @"\s+\d+-?[A-Za-z][A-Za-z]+\.?,?\s*$|\s+\d+\s+[A-Za-z][A-Za-z]+\.?,?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ZipPattern = new(@"\b\d{5}(?:-\d{4})?\b", RegexOptions.Compiled);
    private static readonly Regex ProcessorPrefixPattern = new(
        @"^(?:TST|SQ|SP|TBD|POS|PAYPAL|AMZN(?:\s+MKTP)?)\s*\*+\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VendorMarkerPattern = new(@"[#*＊∗✱﹡⁎]", RegexOptions.Compiled);
    private static readonly HashSet<string> IncompleteVendorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "el", "la", "de", "of", "city", "city of"
    };

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
        var amountCol = FindColumn(header, "amount", "charge", "debit", "usd");
        var debitCol = FindColumn(header, "debit");
        var creditCol = FindExactColumn(header, "credit", "credits");
        var typeCol = FindColumn(header, "type", "transaction type");
        var cardCol = FindColumn(header, "card", "last 4", "last4", "account");

        var startRow = headerIndex >= 0 ? headerIndex + 1 : 0;
        var lines = new List<CreditCardStatementLine>();
        for (var i = startRow; i < grid.Count; i++)
        {
            var cells = grid[i];
            var line = BuildLine(cells, dateCol, vendorCol, amountCol, debitCol, creditCol, typeCol, cardCol, statementCardLastFour, statementCardTypeId);
            if (line != null)
                lines.Add(line);
        }

        return lines;
    }

    private static CreditCardStatementLine? BuildLine(IReadOnlyList<string> cells, int dateCol, int vendorCol, int amountCol, int cardCol, string? statementCardLastFour, int? statementCardTypeId)
        => BuildLine(cells, dateCol, vendorCol, amountCol, -1, -1, -1, cardCol, statementCardLastFour, statementCardTypeId);

    private static CreditCardStatementLine? BuildLine(IReadOnlyList<string> cells, int dateCol, int vendorCol, int amountCol, int debitCol, int creditCol, int typeCol, int cardCol, string? statementCardLastFour, int? statementCardTypeId)
    {
        if (typeCol >= 0 && IsPaymentType(GetCell(cells, typeCol)))
            return null;

        var date = dateCol >= 0 ? ParseDate(GetCell(cells, dateCol)) : FindDate(cells);
        var amount = ResolveChargeAmount(cells, amountCol, debitCol, creditCol);
        var vendor = CleanVendorName(vendorCol >= 0 ? CollectVendorCells(cells, vendorCol) : FindVendor(cells, date, amount));
        var cardLastFour = cardCol >= 0 ? ExtractCardLastFour(GetCell(cells, cardCol)) : ExtractCardLastFour(string.Join(" ", cells));
        var rowText = CleanText(string.Join(" ", cells.Where(cell => !string.IsNullOrWhiteSpace(cell))));

        if (IsSummaryRow(vendor) || IsSummaryRow(rowText) || IsPaymentRow(vendor, rowText))
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

    private static decimal? ResolveChargeAmount(IReadOnlyList<string> cells, int amountCol, int debitCol, int creditCol)
    {
        if (debitCol >= 0 || creditCol >= 0)
        {
            var debit = debitCol >= 0 ? ParseAmount(GetCell(cells, debitCol)) : null;
            var credit = creditCol >= 0 ? ParseAmount(GetCell(cells, creditCol)) : null;
            if (credit is > 0 && debit.GetValueOrDefault() == 0)
                return null;
            if (debit is > 0)
                return debit;
        }

        var amountCell = amountCol >= 0 ? GetCell(cells, amountCol) : null;
        if (IsCreditNotation(amountCell))
            return null;

        var amount = amountCol >= 0 ? ParseAmount(amountCell) : FindAmount(cells);
        return amount.HasValue && amount.Value != 0 ? Math.Abs(amount.Value) : amount;
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
        var hasDate = joined.Contains("date") || joined.Contains("posted");
        var hasCharge = joined.Contains("amount") || joined.Contains("merchant") || joined.Contains("description")
            || joined.Contains("vendor") || joined.Contains("debit") || joined.Contains("payee");
        return hasDate && hasCharge;
    }

    private static bool IsSummaryRow(string? text) => !string.IsNullOrWhiteSpace(text) && SummaryRowPattern.IsMatch(text.Trim());

    private static bool IsPaymentRow(string? vendor, string? description)
    {
        var text = string.Join(" ", new[] { vendor, description }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return !string.IsNullOrWhiteSpace(text) && PaymentRowPattern.IsMatch(text);
    }

    private static bool IsPaymentType(string? value)
    {
        var type = (value ?? string.Empty).Trim().ToLowerInvariant();
        return type is "payment" or "credit" or "refund" or "reversal" or "payment/credit" or "pymt";
    }

    private static bool IsCreditNotation(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length > 0 && (trimmed.Contains('(') && trimmed.Contains(')') || trimmed.EndsWith("CR", StringComparison.OrdinalIgnoreCase));
    }

    public static string? CleanVendorName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var vendor = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? value;
        vendor = ProcessorPrefixPattern.Replace(vendor, string.Empty);
        vendor = StripVendorMarkers(vendor);
        vendor = PhonePattern.Replace(vendor, " ");
        vendor = AddressPattern.Replace(vendor, " ");
        vendor = ZipPattern.Replace(vendor, " ");
        vendor = StripTrailingAddress(vendor);
        var cleaned = FinalizeVendorName(vendor);
        return IsIncompleteVendorName(cleaned) ? null : cleaned;
    }

    private static string StripTrailingAddress(string vendor)
    {
        var current = vendor.Trim();
        var numbered = TrailingNumberedLocationPattern.Replace(current, string.Empty);
        if (!IsIncompleteVendorName(FinalizeVendorName(numbered)))
            current = numbered.Trim();

        var cityState = TrailingCityStatePattern.Replace(current, string.Empty);
        if (!IsIncompleteVendorName(FinalizeVendorName(cityState)))
            return cityState;

        var stateOnly = TrailingStateOnlyPattern.Replace(current, string.Empty);
        if (!IsIncompleteVendorName(FinalizeVendorName(stateOnly)))
            return stateOnly;

        return current;
    }

    private static string StripVendorMarkers(string vendor)
    {
        while (true)
        {
            var match = VendorMarkerPattern.Match(vendor);
            if (!match.Success)
                return vendor;

            var suffix = vendor[(match.Index + match.Length)..];
            vendor = IsJunkVendorSuffix(suffix)
                ? vendor[..match.Index]
                : $"{vendor[..match.Index]} {suffix}";
        }
    }

    private static bool IsJunkVendorSuffix(string suffix)
    {
        var text = suffix.Trim();
        if (text.Length == 0)
            return true;
        if (text.Contains('@'))
            return true;
        if (Regex.IsMatch(text, @"\d{3,}"))
            return true;
        if (!text.Contains(' ') && Regex.IsMatch(text, @"[A-Za-z]") && Regex.IsMatch(text, @"\d"))
            return true;
        return false;
    }

    private static bool IsIncompleteVendorName(string? value)
    {
        var name = (value ?? string.Empty).Trim();
        return name.Length == 0 || IncompleteVendorNames.Contains(name);
    }

    private static string? FinalizeVendorName(string? value)
    {
        var vendor = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim(' ', ',', '-', '/', '|', '*', '#');
        if (vendor.Length == 0)
            return null;

        return string.Join(' ', vendor.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ToTitleCaseWord));
    }

    private static string ToTitleCaseWord(string word)
    {
        return string.Join('-', word.Split('-').Select(part =>
            part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
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

    private static int FindExactColumn(IReadOnlyList<string> header, params string[] names)
    {
        for (var i = 0; i < header.Count; i++)
        {
            var value = (header[i] ?? string.Empty).Trim().ToLowerInvariant();
            if (names.Any(name => value == name || value == name + "s" || value == name + " amount"))
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

    private static string? CollectVendorCells(IReadOnlyList<string> cells, int start)
    {
        var first = CleanText(GetCell(cells, start));
        if (string.IsNullOrWhiteSpace(first) || !IsIncompleteVendorName(first))
            return first;

        var parts = new List<string>();
        for (var i = start; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (string.IsNullOrWhiteSpace(cell) || ParseDate(cell).HasValue || ParseAmount(cell).HasValue)
                break;

            var cleaned = CleanText(cell);
            if (string.IsNullOrWhiteSpace(cleaned) || !cleaned.Any(char.IsLetter))
                break;

            parts.Add(cleaned);
        }

        return parts.Count == 0 ? first : string.Join(" ", parts);
    }

    private static string? FindVendor(IReadOnlyList<string> cells, DateOnly? date, decimal? amount)
    {
        var parts = new List<string>();
        foreach (var cell in cells)
        {
            if (string.IsNullOrWhiteSpace(cell) || ParseDate(cell).HasValue || ParseAmount(cell).HasValue)
            {
                if (parts.Count > 0)
                    break;
                continue;
            }

            var cleaned = CleanText(cell);
            if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Any(char.IsLetter))
                parts.Add(cleaned);
        }

        if (parts.Count > 0)
            return string.Join(" ", parts);

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

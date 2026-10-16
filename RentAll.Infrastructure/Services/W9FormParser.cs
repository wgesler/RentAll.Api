using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Contacts;

namespace RentAll.Infrastructure.Services;

public static class W9FormParser
{
    public const string FederalTaxClassification = "Checked_federal_tax_classification";
    public const string LlcTaxClassification = "LLC_tax_classification_letter";
    public const string SocialSecurityNumber = "Social_security_number";
    public const string EmployerIdentificationNumber = "Employer_identification_number";

    public static readonly string[] QueryFields =
    [
        FederalTaxClassification,
        LlcTaxClassification,
        SocialSecurityNumber,
        EmployerIdentificationNumber
    ];

    private static readonly Regex SsnPattern = new(@"\b(\d{3})-(\d{2})-(\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex EinPattern = new(@"\b(\d{2})-(\d{7})\b", RegexOptions.Compiled);
    private static readonly Regex SelectedBoxPattern = new(@":selected:\s*(?<label>[^\r\n:]{0,140})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MarkedLabelPattern = new(@"(?:^|[^\w])[xX✓☑☒]\s*(?<label>individual\s*/\s*sole\s+proprietor|c\s+corporation|s\s+corporation|partnership|trust\s*/?\s*estate|limited\s+liability\s+company)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BoxedTinPattern = new(@"(\d)\s+(\d)\s+(\d)\s*[-–—]?\s*(\d)\s+(\d)\s*[-–—]?\s*(\d)\s+(\d)\s+(\d)\s+(\d)", RegexOptions.Compiled);

    public static W9DocumentExtraction Parse(IReadOnlyDictionary<string, string> fields, string? fullText)
    {
        var warnings = new List<string>();
        var taxClassificationId = MapFederalTax(
            Field(fields, FederalTaxClassification),
            Field(fields, LlcTaxClassification))
            ?? MapSelectedBoxes(fullText);

        var ssn = Digits9(Field(fields, SocialSecurityNumber));
        var ein = Digits9(Field(fields, EmployerIdentificationNumber));
        if (ssn == null && ein == null)
            (ssn, ein) = FindTinInText(fullText);

        int? ownerIdTypeId = null;
        string? identificationNumber = null;
        if (ssn != null && ein != null)
        {
            if (taxClassificationId is (int)FederalTaxType.IndividualProprietor)
            {
                ownerIdTypeId = (int)OwnerIdType.SocialSecurity;
                identificationNumber = ssn;
            }
            else
            {
                ownerIdTypeId = (int)OwnerIdType.EmployerIdentification;
                identificationNumber = ein;
            }
        }
        else if (ssn != null)
        {
            ownerIdTypeId = (int)OwnerIdType.SocialSecurity;
            identificationNumber = ssn;
        }
        else if (ein != null)
        {
            ownerIdTypeId = (int)OwnerIdType.EmployerIdentification;
            identificationNumber = ein;
        }

        if (!taxClassificationId.HasValue)
            warnings.Add("Federal tax classification was not detected.");
        if (identificationNumber == null)
            warnings.Add("Identification number was not detected.");

        return new W9DocumentExtraction
        {
            FederalTaxClassificationId = taxClassificationId,
            OwnerIdTypeId = ownerIdTypeId,
            IdentificationNumber = identificationNumber,
            Warnings = warnings
        };
    }

    private static string? Field(IReadOnlyDictionary<string, string> fields, string name)
    {
        return fields.TryGetValue(name, out var value) ? value : null;
    }

    private static int? MapFederalTax(string? classification, string? llcLetter)
    {
        var llc = NormalizeLlcLetter(llcLetter);
        var text = Normalize(classification);
        if (llc == 'S' || text.Contains("s corporation") || text.Contains("s corp"))
            return (int)FederalTaxType.SCorporation;
        if (llc == 'C' || text.Contains("c corporation") || text.Contains("c corp"))
            return (int)FederalTaxType.CCorporation;
        if (llc == 'P' || text.Contains("partnership"))
            return (int)FederalTaxType.Partnership;
        if (text.Contains("trust") || text.Contains("estate"))
            return (int)FederalTaxType.TrustEstate;
        if (text.Contains("individual") || text.Contains("sole prop"))
            return (int)FederalTaxType.IndividualProprietor;
        return null;
    }

    private static char? NormalizeLlcLetter(string? value)
    {
        var text = Normalize(value);
        if (text.Length == 0)
            return null;
        if (text is "c" or "s" or "p")
            return char.ToUpperInvariant(text[0]);
        if (text.Contains("c corporation") || text == "c corp")
            return 'C';
        if (text.Contains("s corporation") || text == "s corp")
            return 'S';
        if (text.Contains("partnership"))
            return 'P';
        return null;
    }

    private static int? MapSelectedBoxes(string? fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return null;

        foreach (Match match in SelectedBoxPattern.Matches(fullText))
        {
            var mapped = MapFederalTax(match.Groups["label"].Value, ExtractLlcLetter(match.Groups["label"].Value));
            if (mapped.HasValue)
                return mapped;
        }

        foreach (Match match in MarkedLabelPattern.Matches(fullText))
        {
            var mapped = MapFederalTax(match.Groups["label"].Value, ExtractLlcLetter(match.Groups["label"].Value));
            if (mapped.HasValue)
                return mapped;
        }

        return null;
    }

    private static string? ExtractLlcLetter(string label)
    {
        var match = Regex.Match(label, @"\b([cCsSpP])\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static (string? Ssn, string? Ein) FindTinInText(string? fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return (null, null);

        var ssn = FindBoxedTin(fullText, "Social security number")
            ?? DigitsNearLabel(fullText, "Social security number", "Employer identification")
            ?? DigitsNearLabel(fullText, "Social security", "Employer identification")
            ?? SingleMatch(SsnPattern, fullText, match => match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value);
        var ein = FindBoxedTin(fullText, "Employer identification number")
            ?? DigitsNearLabel(fullText, "Employer identification number", "Part II")
            ?? DigitsNearLabel(fullText, "Employer identification number", "Certification")
            ?? SingleMatch(EinPattern, fullText, match => match.Groups[1].Value + match.Groups[2].Value);
        if (ein != null && ein == ssn)
            ein = null;

        return (ssn, ein);
    }

    private static string? DigitsNearLabel(string text, string label, string stopLabel)
    {
        var start = text.LastIndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        var regionStart = start + label.Length;
        var stop = text.IndexOf(stopLabel, regionStart, StringComparison.OrdinalIgnoreCase);
        var regionLength = stop > regionStart ? Math.Min(220, stop - regionStart) : Math.Min(220, text.Length - regionStart);
        if (regionLength <= 0)
            return null;

        var digits = new string(text.Substring(regionStart, regionLength).Where(char.IsDigit).ToArray());
        return digits.Length >= 9 ? digits[..9] : null;
    }

    private static string? FindBoxedTin(string text, string afterLabel)
    {
        var start = text.LastIndexOf(afterLabel, StringComparison.OrdinalIgnoreCase);
        var match = BoxedTinPattern.Match(text, start < 0 ? 0 : start);
        if (!match.Success)
            return null;

        return string.Concat(Enumerable.Range(1, 9).Select(index => match.Groups[index].Value));
    }

    private static string? SingleMatch(Regex pattern, string text, Func<Match, string> value)
    {
        var matches = pattern.Matches(text)
            .Select(value)
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private static string? Digits9(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 9 ? digits : null;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = Regex.Replace(value.Trim(), @"[ \t]+", " ");
        if (text.Equals("n/a", StringComparison.OrdinalIgnoreCase)
            || text.Equals("na", StringComparison.OrdinalIgnoreCase)
            || text.Equals("none", StringComparison.OrdinalIgnoreCase)
            || text.Contains("applied for", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return text;
    }

    private static string Normalize(string? value)
    {
        var text = Clean(value);
        return text == null ? string.Empty : Regex.Replace(text.ToLowerInvariant(), @"\s+", " ");
    }
}

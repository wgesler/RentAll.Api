namespace RentAll.Domain;

public static class EntityCodeFormatting
{
    public const int NumberDigits = 9;
    public const int LegacyNumberDigits = 6;

    public static string Format(string prefix, int nextNumber)
        => $"{prefix}-{nextNumber.ToString($"D{NumberDigits}")}";

    public static string FormatContact(string prefix, int nextNumber)
        => $"C{prefix}-{nextNumber.ToString($"D{NumberDigits}")}";

    /// <summary>
    /// Returns password candidates for entity-code logins (6- and 9-digit padded forms).
    /// </summary>
    public static IEnumerable<string> GetLoginPasswordAlternates(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            yield break;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in BuildLoginPasswordAlternates(password.Trim()))
        {
            if (seen.Add(candidate))
                yield return candidate;
        }
    }

    public static string? PadToNineDigits(string? code)
    {
        if (!TryParseFirstNumericSegment(code, out var prefix, out var numValue, out var suffix))
            return null;

        var numSegment = numValue.ToString($"D{NumberDigits}");
        return $"{prefix}-{numSegment}{suffix}";
    }

    public static string? FormatLegacySixDigitSegment(string? code)
    {
        if (!TryParseFirstNumericSegment(code, out var prefix, out var numValue, out var suffix))
            return null;

        return $"{prefix}-{numValue.ToString($"D{LegacyNumberDigits}")}{suffix}";
    }

    public static bool CodesMatch(string? left, string? right)
    {
        var leftTrimmed = left?.Trim();
        var rightTrimmed = right?.Trim();
        if (string.IsNullOrWhiteSpace(leftTrimmed) || string.IsNullOrWhiteSpace(rightTrimmed))
            return false;

        if (string.Equals(leftTrimmed, rightTrimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!TryParseFirstNumericSegment(leftTrimmed, out var leftPrefix, out var leftNumber, out var leftSuffix)
            || !TryParseFirstNumericSegment(rightTrimmed, out var rightPrefix, out var rightNumber, out var rightSuffix))
        {
            return false;
        }

        return leftNumber == rightNumber
            && string.Equals(leftPrefix, rightPrefix, StringComparison.OrdinalIgnoreCase)
            && string.Equals(leftSuffix, rightSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> BuildLoginPasswordAlternates(string trimmed)
    {
        yield return trimmed;

        var padded = PadToNineDigits(trimmed);
        if (padded != null)
            yield return padded;

        var legacy = FormatLegacySixDigitSegment(trimmed);
        if (legacy != null)
            yield return legacy;
    }

    private static bool TryParseFirstNumericSegment(
        string? code,
        out string prefix,
        out int numValue,
        out string suffix)
    {
        prefix = string.Empty;
        suffix = string.Empty;
        numValue = 0;

        var trimmed = code?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        var firstHyphen = trimmed.IndexOf('-');
        if (firstHyphen <= 0)
            return false;

        prefix = trimmed[..firstHyphen];
        if (prefix.Any(static c => !char.IsLetter(c)))
            return false;

        var remainder = trimmed[(firstHyphen + 1)..];
        string numSegment;
        var nextHyphen = remainder.IndexOf('-');
        if (nextHyphen < 0)
        {
            numSegment = remainder;
            suffix = string.Empty;
        }
        else
        {
            numSegment = remainder[..nextHyphen];
            suffix = remainder[nextHyphen..];
        }

        if (string.IsNullOrEmpty(numSegment) || numSegment.Any(static c => !char.IsDigit(c)))
            return false;

        return int.TryParse(numSegment, out numValue);
    }
}

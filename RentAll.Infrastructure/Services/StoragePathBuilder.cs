namespace RentAll.Infrastructure.Services;

public static class StoragePathBuilder
{
    public static string BuildDirectory(Guid organizationId, string? officeName, string category, string? propertyCode)
    {
        var scope = SplitStorageScope(officeName);
        var orgSegment = $"{organizationId:N}".ToLowerInvariant();
        var officeSegment = OfficeFolderName(scope.OfficeName);
        var categorySegment = SanitizeSegment(string.IsNullOrWhiteSpace(scope.Category) ? category : scope.Category, "files", 40);
        var propertySource = string.IsNullOrWhiteSpace(propertyCode) ? scope.PropertyCode : propertyCode;
        if (string.IsNullOrWhiteSpace(propertySource))
            return $"{orgSegment}/{officeSegment}/{categorySegment}";

        var propertySegment = SanitizeSegment(propertySource, "unknown-property", 80);
        return $"{orgSegment}/{officeSegment}/{categorySegment}/{propertySegment}";
    }

    // DO NOT MODIFY the office name. Lowercase only.
    // office.Name is the folder, as the office stored it. Hyphens stay hyphens. Spaces stay spaces. Numbers stay numbers.
    // Do not strip, replace, or rewrite any character. A new spelling creates a second folder for that office.
    // A blank name is the only exception: use "global".
    public static string OfficeFolderName(string? officeName)
    {
        var trimmed = (officeName ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(trimmed) ? "global" : trimmed;
    }

    public static string SanitizeSegment(string? value, string fallback, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var trimmed = value.Trim().ToLowerInvariant();
        var chars = trimmed.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ' ? c : '-').ToArray();
        var normalized = new string(chars);
        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

        normalized = normalized.Trim('-', '_', '.', ' ');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = fallback;

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static (string OfficeName, string? Category, string? PropertyCode) SplitStorageScope(string? officeName)
    {
        if (string.IsNullOrWhiteSpace(officeName))
            return ("global", null, null);

        var parts = officeName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && IsPropertyPictureCategory(parts[1]))
            return (parts[0], parts[1], parts[2]);

        return (officeName.Trim(), null, null);
    }

    private static bool IsPropertyPictureCategory(string value)
    {
        return value.Equals("listings", StringComparison.OrdinalIgnoreCase)
            || value.Equals("inspection", StringComparison.OrdinalIgnoreCase);
    }
}

using System.Text.RegularExpressions;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Brandings;

public class UpdateBrandingDto
{
    public Guid OrganizationId { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public string HeaderBackgroundColor { get; set; } = string.Empty;
    public string HeaderTextColor { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public string? CollapsedLogoPath { get; set; }
    public FileDetails? CollapsedFileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (!IsValidHexColor(PrimaryColor))
            return (false, "PrimaryColor must be a 6-character hexadecimal value (e.g., #3f51b5)");

        if (!IsValidHexColor(AccentColor))
            return (false, "AccentColor must be a 6-character hexadecimal value (e.g., #ae1f66)");

        if (!IsValidHexColor(HeaderBackgroundColor))
            return (false, "HeaderBackgroundColor must be a 6-character hexadecimal value (e.g., #3f51b5)");

        if (!IsValidHexColor(HeaderTextColor))
            return (false, "HeaderTextColor must be a 6-character hexadecimal value (e.g., #ffffff)");

        return (true, null);
    }

    public Branding ToModel()
    {
        return new Branding
        {
            OrganizationId = OrganizationId,
            PrimaryColor = NormalizeHexColor(PrimaryColor),
            AccentColor = NormalizeHexColor(AccentColor),
            HeaderBackgroundColor = NormalizeHexColor(HeaderBackgroundColor),
            HeaderTextColor = NormalizeHexColor(HeaderTextColor),
            LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath.Trim(),
            CollapsedLogoPath = string.IsNullOrWhiteSpace(CollapsedLogoPath) ? null : CollapsedLogoPath.Trim()
        };
    }

    private static bool IsValidHexColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
            normalized = $"#{normalized}";

        return Regex.IsMatch(normalized, "^#[0-9A-Fa-f]{6}$");
    }

    private static string NormalizeHexColor(string value)
    {
        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
            normalized = $"#{normalized}";

        return normalized.ToLowerInvariant();
    }
}

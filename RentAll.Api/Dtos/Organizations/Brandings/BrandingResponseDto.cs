using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Brandings;

public class BrandingResponseDto
{
    public Guid OrganizationId { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public string HeaderBackgroundColor { get; set; } = string.Empty;
    public string HeaderTextColor { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? CollapsedLogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public FileDetails? CollapsedFileDetails { get; set; }

    public BrandingResponseDto(Branding branding)
    {
        OrganizationId = branding.OrganizationId;
        PrimaryColor = branding.PrimaryColor;
        AccentColor = branding.AccentColor;
        HeaderBackgroundColor = branding.HeaderBackgroundColor;
        HeaderTextColor = branding.HeaderTextColor;
        LogoPath = branding.LogoPath;
        CollapsedLogoPath = branding.CollapsedLogoPath;
    }
}

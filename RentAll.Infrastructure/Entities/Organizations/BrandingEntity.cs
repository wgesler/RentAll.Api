namespace RentAll.Infrastructure.Entities.Organizations;

public class BrandingEntity
{
    public Guid OrganizationId { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public string HeaderBackgroundColor { get; set; } = string.Empty;
    public string HeaderTextColor { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? CollapsedLogoPath { get; set; }
}

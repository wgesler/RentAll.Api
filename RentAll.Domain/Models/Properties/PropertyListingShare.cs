namespace RentAll.Domain.Models.Properties;

public class PropertyListingShare
{
    public Guid ShareId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
    public bool IsActive { get; set; }
}

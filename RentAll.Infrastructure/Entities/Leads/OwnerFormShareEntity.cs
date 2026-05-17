namespace RentAll.Infrastructure.Entities.Leads;

public class OwnerFormShareEntity
{
    public Guid ShareId { get; set; }
    public int OwnerId { get; set; }
    public Guid OrganizationId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
}

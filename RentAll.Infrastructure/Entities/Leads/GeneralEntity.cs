namespace RentAll.Infrastructure.Entities.Leads;

public class GeneralEntity
{
    public int GeneralId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int LeadStateId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneMobile { get; set; }
    public string? Message { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

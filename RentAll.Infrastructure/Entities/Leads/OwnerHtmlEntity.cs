namespace RentAll.Infrastructure.Entities.Leads;

public class OwnerHtmlEntity
{
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OwnerAgreement { get; set; } = string.Empty;
    public string DirectDeposit { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

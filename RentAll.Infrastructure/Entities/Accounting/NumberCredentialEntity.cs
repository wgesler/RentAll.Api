namespace RentAll.Infrastructure.Entities.Accounting;

public class NumberCredentialEntity
{
    public int NumberCredentialId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string SessKey { get; set; } = string.Empty;
    public int MerchId { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

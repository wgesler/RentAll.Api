namespace RentAll.Infrastructure.Entities.Accounting;

public class ChartOfAccountEntity
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int AccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public int AccountTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSubaccount { get; set; }
    public int? SubAccountId { get; set; }
    public string? Description { get; set; }
    public string? Note { get; set; }
}

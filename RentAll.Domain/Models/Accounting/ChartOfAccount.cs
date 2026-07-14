using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ChartOfAccount
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int AccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSubaccount { get; set; }
    public int? SubAccountId { get; set; }
    public string? Description { get; set; }
    public decimal? EndingBalance { get; set; }
    public DateOnly? StatementDate { get; set; }
    public string? Note { get; set; }
}

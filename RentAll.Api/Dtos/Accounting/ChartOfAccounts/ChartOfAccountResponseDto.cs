using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.ChartOfAccounts;

public class ChartOfAccountResponseDto
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

    public ChartOfAccountResponseDto(ChartOfAccount chartOfAccount)
    {
        OrganizationId = chartOfAccount.OrganizationId;
        OfficeId = chartOfAccount.OfficeId;
        AccountId = chartOfAccount.AccountId;
        AccountNo = chartOfAccount.AccountNo;
        AccountTypeId = (int)chartOfAccount.AccountType;
        Name = chartOfAccount.Name;
        IsSubaccount = chartOfAccount.IsSubaccount;
        SubAccountId = chartOfAccount.SubAccountId;
        Description = chartOfAccount.Description;
        Note = chartOfAccount.Note;
    }
}

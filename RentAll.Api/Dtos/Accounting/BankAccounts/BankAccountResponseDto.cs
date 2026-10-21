using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Accounting.BankAccounts;

public class BankAccountResponseDto
{
    public int BankAccountId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankPhone { get; set; }
    public string? BankRouting { get; set; }
    public string? BankSwiftCode { get; set; }
    public string? BankAddress { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountNoLastFour { get; set; }
    public int? AccountChartOfAccountId { get; set; }
    public int CurrentCheckNumber { get; set; }
    public string? CheckStockPath { get; set; }
    public FileDetails? CheckStockFileDetails { get; set; }
    public string? CheckHtml { get; set; }
    public bool UseOnInvoices { get; set; }

    public BankAccountResponseDto(BankAccount bankAccount)
    {
        BankAccountId = bankAccount.BankAccountId;
        OrganizationId = bankAccount.OrganizationId;
        OfficeId = bankAccount.OfficeId;
        BankName = bankAccount.BankName;
        BankPhone = bankAccount.BankPhone;
        BankRouting = bankAccount.BankRouting;
        BankSwiftCode = bankAccount.BankSwiftCode;
        BankAddress = bankAccount.BankAddress;
        AccountNo = bankAccount.AccountNo;
        AccountNoLastFour = bankAccount.AccountNoLastFour;
        AccountChartOfAccountId = bankAccount.AccountChartOfAccountId;
        CurrentCheckNumber = bankAccount.CurrentCheckNumber;
        CheckStockPath = bankAccount.CheckStockPath;
        CheckHtml = bankAccount.CheckHtml;
        UseOnInvoices = bankAccount.UseOnInvoices;
    }
}

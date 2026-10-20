using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Accounting.Banks;

public class BankResponseDto
{
    public int BankId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankPhone { get; set; }
    public string? BankRouting { get; set; }
    public string? BankAccount { get; set; }
    public string? BankSwiftCode { get; set; }
    public string? BankAddress { get; set; }
    public int CurrentCheckNumber { get; set; }
    public string? CheckStockPath { get; set; }
    public FileDetails? CheckStockFileDetails { get; set; }
    public string? CheckHtml { get; set; }
    public bool UseOnInvoices { get; set; }

    public BankResponseDto(Bank bank)
    {
        BankId = bank.BankId;
        OrganizationId = bank.OrganizationId;
        OfficeId = bank.OfficeId;
        BankName = bank.BankName;
        BankPhone = bank.BankPhone;
        BankRouting = bank.BankRouting;
        BankAccount = bank.BankAccount;
        BankSwiftCode = bank.BankSwiftCode;
        BankAddress = bank.BankAddress;
        CurrentCheckNumber = bank.CurrentCheckNumber;
        CheckStockPath = bank.CheckStockPath;
        CheckHtml = bank.CheckHtml;
        UseOnInvoices = bank.UseOnInvoices;
    }
}

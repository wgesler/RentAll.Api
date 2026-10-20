namespace RentAll.Domain.Models;

public class BankAccount
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
    public int CurrentCheckNumber { get; set; } = 1;
    public string? CheckStockPath { get; set; }
    public string? CheckHtml { get; set; }
    public bool UseOnInvoices { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

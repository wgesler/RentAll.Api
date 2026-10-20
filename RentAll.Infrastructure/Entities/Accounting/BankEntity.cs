namespace RentAll.Infrastructure.Entities.Accounting;

public class BankEntity
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
    public string? CheckHtml { get; set; }
    public bool UseOnInvoices { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

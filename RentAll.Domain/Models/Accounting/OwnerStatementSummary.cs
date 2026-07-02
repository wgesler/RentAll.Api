namespace RentAll.Domain.Models;

public class OwnerStatementSummary
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Expected { get; set; }
    public decimal PrePaid { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal WorkingCapitalBalanceDue { get; set; }
    public decimal OwnerPayment { get; set; }
    public decimal EndingBalance { get; set; }
}

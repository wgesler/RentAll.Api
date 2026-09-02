namespace RentAll.Domain.Models;

public class OwnerStatementMonthCloseLine
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string OwnerNameLine { get; set; } = string.Empty;
    public decimal ClosingBalance { get; set; }
}

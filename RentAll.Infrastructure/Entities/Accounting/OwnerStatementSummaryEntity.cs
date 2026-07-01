namespace RentAll.Infrastructure.Entities.Accounting;

public class OwnerStatementSummaryEntity
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance { get; set; }
    public decimal WorkingCapital { get; set; }
}

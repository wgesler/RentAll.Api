namespace RentAll.Infrastructure.Entities.Accounting;

public class TransferSplitEntity
{
    public int TransferSplitId { get; set; }
    public Guid TransferId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public string? PropertyCode { get; set; }
    public string? ReservationCode { get; set; }
    public string? ContactName { get; set; }
    public int? ChartOfAccountId { get; set; }
    public string ChartOfAccountDisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

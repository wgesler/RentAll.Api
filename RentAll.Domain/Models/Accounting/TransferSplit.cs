namespace RentAll.Domain.Models;

public class TransferSplit
{
    public int TransferSplitId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public string? PropertyCode { get; set; }
    public int? ChartOfAccountId { get; set; }
    public string ChartOfAccountDisplayName { get; set; } = string.Empty;
}

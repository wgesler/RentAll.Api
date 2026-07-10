namespace RentAll.Domain.Models;

public class DepositSplit
{
    public int DepositSplitId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public string? PropertyCode { get; set; }
    public int? ChartOfAccountId { get; set; }
    public string ChartOfAccountDisplayName { get; set; } = string.Empty;
}

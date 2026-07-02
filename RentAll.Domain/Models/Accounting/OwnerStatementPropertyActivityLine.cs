namespace RentAll.Domain.Models;

public class OwnerStatementPropertyActivityLine
{
    public Guid? ActivityId { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedIncome { get; set; }
    public decimal ReceivedIncome { get; set; }
    public decimal Expenses { get; set; }
}

namespace RentAll.Domain.Models;

public class OwnerStatementJournalEntryLineGetCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? ExpectedAccountId { get; set; }
    public int? ActualAccountId { get; set; }
    public int? PrePaidAccountId { get; set; }
    public int? ExpenseAccountId { get; set; }
}

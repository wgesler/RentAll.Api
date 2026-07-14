namespace RentAll.Domain.Models;

public class ReconcileJournalEntryLineMark
{
    public Guid JournalEntryLineId { get; set; }
    public bool IsCleared { get; set; }
}

public class SaveReconcileMarksRequest
{
    public int OfficeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public List<ReconcileJournalEntryLineMark> Lines { get; set; } = new();
}

public class CompleteReconcileRequest : SaveReconcileMarksRequest
{
    public decimal EndingBalance { get; set; }
    public DateOnly StatementDate { get; set; }
}

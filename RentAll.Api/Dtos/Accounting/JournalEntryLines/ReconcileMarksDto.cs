namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

using RentAll.Domain.Models;

public class ReconcileJournalEntryLineMarkDto
{
    public Guid JournalEntryLineId { get; set; }
    public bool IsCleared { get; set; }
}

public class SaveReconcileMarksDto
{
    public int OfficeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public List<ReconcileJournalEntryLineMarkDto> Lines { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (Lines == null || Lines.Count == 0)
            return (false, "At least one journal entry line is required");

        if (Lines.Any(line => line.JournalEntryLineId == Guid.Empty))
            return (false, "JournalEntryLineId is required for each line");

        return (true, null);
    }

    public SaveReconcileMarksRequest ToModel()
    {
        return new SaveReconcileMarksRequest
        {
            OfficeId = OfficeId,
            ChartOfAccountId = ChartOfAccountId,
            Lines = Lines.Select(line => new ReconcileJournalEntryLineMark
            {
                JournalEntryLineId = line.JournalEntryLineId,
                IsCleared = line.IsCleared
            }).ToList()
        };
    }
}

public class CompleteReconcileDto : SaveReconcileMarksDto
{
    public decimal EndingBalance { get; set; }
    public DateOnly StatementDate { get; set; }

    public new (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        var (isValid, errorMessage) = base.IsValid(currentOffices);
        if (!isValid)
            return (false, errorMessage);

        if (StatementDate == default)
            return (false, "StatementDate is required");

        return (true, null);
    }

    public CompleteReconcileRequest ToCompleteModel()
    {
        return new CompleteReconcileRequest
        {
            OfficeId = OfficeId,
            ChartOfAccountId = ChartOfAccountId,
            Lines = Lines.Select(line => new ReconcileJournalEntryLineMark
            {
                JournalEntryLineId = line.JournalEntryLineId,
                IsCleared = line.IsCleared
            }).ToList(),
            EndingBalance = EndingBalance,
            StatementDate = StatementDate
        };
    }
}

namespace RentAll.Api.Dtos.Accounting.CheckPrint;

public class AssignCheckNumbersDto
{
    public int OfficeId { get; set; }
    public int StartingCheckNumber { get; set; }
    public List<Guid> JournalEntryIds { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (StartingCheckNumber < 1)
            return (false, "StartingCheckNumber must be at least 1");

        if (JournalEntryIds == null || JournalEntryIds.Count == 0)
            return (false, "At least one journal entry is required");

        if (JournalEntryIds.Any(id => id == Guid.Empty))
            return (false, "JournalEntryIds cannot contain empty values");

        return (true, null);
    }
}

public class CheckPrintAssignmentDto
{
    public Guid JournalEntryId { get; set; }
    public string CheckNumber { get; set; } = string.Empty;

    public CheckPrintAssignmentDto(Guid journalEntryId, string checkNumber)
    {
        JournalEntryId = journalEntryId;
        CheckNumber = checkNumber;
    }
}

public class AssignCheckNumbersResponseDto
{
    public List<CheckPrintAssignmentDto> Assignments { get; set; } = new();
    public int NextCheckNumber { get; set; }

    public AssignCheckNumbersResponseDto(IEnumerable<CheckPrintAssignmentDto> assignments, int nextCheckNumber)
    {
        Assignments = assignments.ToList();
        NextCheckNumber = nextCheckNumber;
    }
}

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class SyncJournalEntriesRequestDto
{
    public int[] OfficeIds { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds != null && OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        return (true, null);
    }
}

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class StartDocumentTypeJournalEntrySyncRequestDto
{
    public int[] OfficeIds { get; set; } = [];
    public string SyncType { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds != null && OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (string.IsNullOrWhiteSpace(SyncType))
            return (false, "Sync type is required");

        return (true, null);
    }
}

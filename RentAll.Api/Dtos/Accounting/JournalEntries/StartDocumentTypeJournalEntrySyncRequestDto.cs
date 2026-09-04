namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class StartDocumentTypeJournalEntrySyncRequestDto
{
    public int[] OfficeIds { get; set; } = [];
    public string SyncType { get; set; } = string.Empty;
    public Guid[] DocumentIds { get; set; } = [];
    public int? PaymentKindId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds != null && OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (string.IsNullOrWhiteSpace(SyncType))
            return (false, "Sync type is required");

        if (DocumentIds != null && DocumentIds.Any(id => id == Guid.Empty))
            return (false, "Each document ID must be a valid GUID");

        return (true, null);
    }
}

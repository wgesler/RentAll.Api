namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class StartDocumentTypeJournalEntrySyncRequestDto
{
    public int[] OfficeIds { get; set; } = [];
    public string SyncType { get; set; } = string.Empty;
    public string[] DocumentIds { get; set; } = [];
    public int? PaymentKindId { get; set; }
    public bool HealthFix { get; set; }

    public Guid[] ResolveDocumentIds()
    {
        return (DocumentIds ?? [])
            .Select(id => Guid.TryParse(id?.Trim(), out var parsed) ? parsed : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds != null && OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (string.IsNullOrWhiteSpace(SyncType))
            return (false, "Sync type is required");

        foreach (var id in DocumentIds ?? [])
        {
            if (!Guid.TryParse(id?.Trim(), out var parsed) || parsed == Guid.Empty)
                return (false, "Each document ID must be a valid GUID");
        }

        if (HealthFix && ResolveDocumentIds().Length == 0)
            return (false, "At least one document ID is required for health fix.");

        return (true, null);
    }
}

namespace RentAll.Domain.Models;

public class JournalEntrySyncProgress
{
    public string SyncType { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public string Status { get; set; } = string.Empty;
}

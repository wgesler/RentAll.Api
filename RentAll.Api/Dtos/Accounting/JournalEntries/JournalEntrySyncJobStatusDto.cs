namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class JournalEntrySyncJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public bool IsCompleted { get; set; }
    public string? Message { get; set; }
    public List<JournalEntrySyncJobTypeStatusDto> Types { get; set; } = [];
}

public class JournalEntrySyncJobTypeStatusDto
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public string Status { get; set; } = "Pending";
}

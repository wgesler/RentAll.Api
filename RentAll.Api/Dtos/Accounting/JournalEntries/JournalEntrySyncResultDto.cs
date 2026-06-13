using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class JournalEntrySyncResultDto
{
    public int DocumentsProcessed { get; set; }
    public int JournalEntriesCreated { get; set; }
    public int JournalEntriesSkipped { get; set; }
    public int JournalEntriesDeleted { get; set; }
    public List<string> Errors { get; set; } = new();

    public JournalEntrySyncResultDto()
    {
    }

    public JournalEntrySyncResultDto(JournalEntrySyncResult result)
    {
        DocumentsProcessed = result.DocumentsProcessed;
        JournalEntriesCreated = result.JournalEntriesCreated;
        JournalEntriesSkipped = result.JournalEntriesSkipped;
        JournalEntriesDeleted = result.JournalEntriesDeleted;
        Errors = result.Errors;
    }
}

namespace RentAll.Domain.Models;

public class JournalEntrySyncResult
{
    public int DocumentsProcessed { get; set; }
    public int JournalEntriesCreated { get; set; }
    public int JournalEntriesSkipped { get; set; }
    public int JournalEntriesDeleted { get; set; }
    public List<string> Errors { get; set; } = new();
}

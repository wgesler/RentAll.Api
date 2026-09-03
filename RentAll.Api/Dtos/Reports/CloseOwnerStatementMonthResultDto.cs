namespace RentAll.Api.Dtos.Reports;

public class CloseOwnerStatementMonthResultDto
{
    public int PropertiesProcessed { get; set; }
    public int JournalEntriesCreated { get; set; }
    public int JournalEntriesUpdated { get; set; }
    public bool OwnerApSoftCloseQueued { get; set; }

    public CloseOwnerStatementMonthResultDto()
    {
    }

    public CloseOwnerStatementMonthResultDto(CloseOwnerStatementMonthResult result)
    {
        PropertiesProcessed = result.PropertiesProcessed;
        JournalEntriesCreated = result.JournalEntriesCreated;
        JournalEntriesUpdated = result.JournalEntriesUpdated;
        OwnerApSoftCloseQueued = result.OwnerApSoftCloseQueued;
    }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class CloseOwnerStatementMonthResultDto
{
    public int PropertiesProcessed { get; set; }
    public int JournalEntriesCreated { get; set; }
    public int JournalEntriesUpdated { get; set; }

    public CloseOwnerStatementMonthResultDto()
    {
    }

    public CloseOwnerStatementMonthResultDto(CloseOwnerStatementMonthResult result)
    {
        PropertiesProcessed = result.PropertiesProcessed;
        JournalEntriesCreated = result.JournalEntriesCreated;
        JournalEntriesUpdated = result.JournalEntriesUpdated;
    }
}

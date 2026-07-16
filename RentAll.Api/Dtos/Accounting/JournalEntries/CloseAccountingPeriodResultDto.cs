using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class CloseAccountingPeriodResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int? ClosedDateId { get; set; }
    public List<string> Errors { get; set; } = new();

    public CloseAccountingPeriodResultDto()
    {
    }

    public CloseAccountingPeriodResultDto(CloseAccountingPeriodResult result)
    {
        SuccessCount = result.SuccessCount;
        FailedCount = result.FailedCount;
        ClosedDateId = result.ClosedDateId;
        Errors = result.Errors;
    }
}

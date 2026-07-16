namespace RentAll.Domain.Models;

public class CloseAccountingPeriodResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int? ClosedDateId { get; set; }
    public List<string> Errors { get; set; } = new();
}

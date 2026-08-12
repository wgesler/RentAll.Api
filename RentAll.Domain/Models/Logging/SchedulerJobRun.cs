namespace RentAll.Domain.Models;

public class SchedulerJobRun
{
    public string JobName { get; set; } = string.Empty;
    public DateOnly LastRanOn { get; set; }
    public DateTimeOffset LastRanAt { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

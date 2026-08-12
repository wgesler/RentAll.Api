namespace RentAll.Infrastructure.Entities.Logging;

public class SchedulerJobRunEntity
{
    public string JobName { get; set; } = string.Empty;
    public DateOnly LastRanOn { get; set; }
    public DateTimeOffset LastRanAt { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

public class SchedulerJobRunClaimEntity : SchedulerJobRunEntity
{
    public bool Claimed { get; set; }
}

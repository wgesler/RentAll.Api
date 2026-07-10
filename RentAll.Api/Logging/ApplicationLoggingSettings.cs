namespace RentAll.Api.Logging;

public class ApplicationLoggingSettings
{
    public bool Enabled { get; set; } = true;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Error;
    public int QueueCapacity { get; set; } = 2000;
    public bool RetentionEnabled { get; set; } = true;
    public int RetentionDays { get; set; } = 30;
}

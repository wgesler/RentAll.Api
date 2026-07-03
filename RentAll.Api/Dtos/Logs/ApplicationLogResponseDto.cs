namespace RentAll.Api.Dtos.Logs;

public class ApplicationLogResponseDto
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int? EventId { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string? TraceId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public ApplicationLogResponseDto(ApplicationLog applicationLog)
    {
        Id = applicationLog.Id;
        Level = applicationLog.Level;
        Category = applicationLog.Category;
        EventId = applicationLog.EventId;
        OrganizationId = applicationLog.OrganizationId;
        OfficeId = applicationLog.OfficeId;
        TraceId = applicationLog.TraceId;
        Message = applicationLog.Message;
        Exception = applicationLog.Exception;
        Properties = applicationLog.Properties;
        CreatedOn = applicationLog.CreatedOn;
    }
}

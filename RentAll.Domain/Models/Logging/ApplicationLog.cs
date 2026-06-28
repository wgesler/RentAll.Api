namespace RentAll.Domain.Models;

public class ApplicationLog
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
}

namespace RentAll.Domain.Models;

public class PropertyUploadLog
{
    public int Id { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ImportId { get; set; }
    public int? PhotoId { get; set; }
    public string? Url { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
}

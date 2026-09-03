namespace RentAll.Api.Services;

public sealed class ExternalPropertyApiAttemptLog
{
    public Guid OrganizationId { get; init; }
    public int? OfficeId { get; init; }
    public Guid? VendorId { get; init; }
    public Guid? PropertyId { get; init; }
    public string? PropertyCode { get; init; }
    public Guid? ImportId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

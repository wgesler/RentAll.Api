namespace RentAll.Infrastructure.Entities.Logging;

public class DatabaseErrorLogEntity
{
    public int Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string? TableName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

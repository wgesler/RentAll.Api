namespace RentAll.Api.Dtos.Logs;

public class DatabaseErrorLogResponseDto
{
    public int Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string? TableName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public DatabaseErrorLogResponseDto(DatabaseErrorLog databaseErrorLog)
    {
        Id = databaseErrorLog.Id;
        OrganizationId = databaseErrorLog.OrganizationId;
        OfficeId = databaseErrorLog.OfficeId;
        TableName = databaseErrorLog.TableName;
        Message = databaseErrorLog.Message;
        Exception = databaseErrorLog.Exception;
        CreatedOn = databaseErrorLog.CreatedOn;
    }
}

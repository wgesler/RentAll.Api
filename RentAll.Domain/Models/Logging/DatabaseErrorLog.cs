namespace RentAll.Domain.Models;

public class DatabaseErrorLog
{
    public int Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string? TableName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

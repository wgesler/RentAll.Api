namespace RentAll.Domain.Models;

public class DocumentGetCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? DocumentTypeIds { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

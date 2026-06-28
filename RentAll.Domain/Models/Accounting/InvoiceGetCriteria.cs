namespace RentAll.Domain.Models;

public class InvoiceGetCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public Guid? ReservationId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? InvoiceCode { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeInactive { get; set; }
    public bool IncludePaid { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

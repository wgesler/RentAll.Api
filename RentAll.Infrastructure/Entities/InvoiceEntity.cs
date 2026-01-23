namespace RentAll.Infrastructure.Entities;

public class InvoiceEntity
{
	public Guid InvoiceId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public string InvoiceName { get; set; } = string.Empty;
	public Guid? ReservationId { get; set; }
	public string? ReservationCode { get; set; }
	public DateTimeOffset InvoiceDate { get; set; }
	public DateTimeOffset? DueDate { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal PaidAmount { get; set; }
	public string? Notes { get; set; }
	public string? Lines { get; set; }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class InvoiceResponseDto
{
	public Guid InvoiceId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public Guid? ReservationId { get; set; }
	public string? ReservationCode { get; set; }
	public Guid ContactId { get; set; }
	public string ContactName { get; set; } = string.Empty;
	public DateTimeOffset InvoiceDate { get; set; }
	public DateTimeOffset? DueDate { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal PaidAmount { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

	public InvoiceResponseDto(Invoice invoice)
	{
		InvoiceId = invoice.InvoiceId;
		OrganizationId = invoice.OrganizationId;
		OfficeId = invoice.OfficeId;
		OfficeName = invoice.OfficeName;
		ReservationId = invoice.ReservationId;
		ReservationCode = invoice.ReservationCode;
		ContactId = invoice.ContactId;
		ContactName = invoice.ContactName;
		InvoiceDate = invoice.InvoiceDate;
		DueDate = invoice.DueDate;
		TotalAmount = invoice.TotalAmount;
		PaidAmount = invoice.PaidAmount;
		Notes = invoice.Notes;
		IsActive = invoice.IsActive;
		CreatedOn = invoice.CreatedOn;
		CreatedBy = invoice.CreatedBy;
		ModifiedOn = invoice.ModifiedOn;
		ModifiedBy = invoice.ModifiedBy;
	}
}

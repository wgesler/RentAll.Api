using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class InvoiceResponseDto
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
	public bool IsActive { get; set; }
	public List<LedgerLine> LedgerLines { get; set; } = new List<LedgerLine>();

	public InvoiceResponseDto(Invoice invoice)
	{
		InvoiceId = invoice.InvoiceId;
		OrganizationId = invoice.OrganizationId;
		OfficeId = invoice.OfficeId;
		OfficeName = invoice.OfficeName;
		InvoiceName = invoice.InvoiceName;
		ReservationId = invoice.ReservationId;
		ReservationCode = invoice.ReservationCode;
		InvoiceDate = invoice.InvoiceDate;
		DueDate = invoice.DueDate;
		TotalAmount = invoice.TotalAmount;
		PaidAmount = invoice.PaidAmount;
		Notes = invoice.Notes;
		LedgerLines = invoice.LedgerLines;
		IsActive = invoice.IsActive;
	}
}

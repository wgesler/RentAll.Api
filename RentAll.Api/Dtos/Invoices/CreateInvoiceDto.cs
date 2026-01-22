using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class CreateInvoiceDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public Guid? ReservationId { get; set; }
	public DateTimeOffset InvoiceDate { get; set; }
	public DateTimeOffset? DueDate { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal PaidAmount { get; set; }
	public string? Notes { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		return (true, null);
	}

	public Invoice ToModel()
	{
		return new Invoice
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			ReservationId = ReservationId,
			InvoiceDate = InvoiceDate,
			DueDate = DueDate,
			TotalAmount = TotalAmount,
			PaidAmount = PaidAmount,
			Notes = Notes
		};
	}
}

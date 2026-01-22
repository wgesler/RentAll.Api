using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class UpdateInvoiceDto
{
	public Guid InvoiceId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public Guid? ReservationId { get; set; }
	public Guid ContactId { get; set; }
	public DateTimeOffset InvoiceDate { get; set; }
	public DateTimeOffset? DueDate { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal PaidAmount { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
	{
		if (id == Guid.Empty)
			return (false, "Invoice ID is required");

		if (InvoiceId != id)
			return (false, "Invoice ID mismatch");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (ContactId == Guid.Empty)
			return (false, "ContactId is required");

		return (true, null);
	}

	public Invoice ToModel(Guid currentUser)
	{
		return new Invoice
		{
			InvoiceId = InvoiceId,
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			ReservationId = ReservationId,
			ContactId = ContactId,
			InvoiceDate = InvoiceDate,
			DueDate = DueDate,
			TotalAmount = TotalAmount,
			PaidAmount = PaidAmount,
			Notes = Notes,
			IsActive = IsActive,
			ModifiedBy = currentUser
		};
	}
}

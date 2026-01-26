using RentAll.Api.Dtos.LedgerLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class UpdateInvoiceDto
{
	public Guid InvoiceId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public string InvoiceName { get; set; } = string.Empty;
	public Guid ReservationId { get; set; }
	public string ReservationCode { get; set; } = string.Empty;
	public DateTimeOffset InvoiceDate { get; set; }
	public DateTimeOffset? DueDate { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal PaidAmount { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }
	public List<CreateLedgerLineDto> LedgerLines { get; set; } = new List<CreateLedgerLineDto>();

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
	{
		if (id == Guid.Empty)
			return (false, "Invoice ID is required");

		if (InvoiceId != id)
			return (false, "Invoice ID mismatch");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (OfficeId <= 0)
			return (false, "OfficeId is required");

		if (string.IsNullOrWhiteSpace(OfficeName))
			return (false, "OfficeName is required");

		if (string.IsNullOrWhiteSpace(InvoiceName))
			return (false, "InvoiceName is required");

		if (InvoiceDate == default)
			return (false, "InvoiceDate is required");

		if (TotalAmount < 0)
			return (false, "TotalAmount cannot be negative");

		if (PaidAmount < 0)
			return (false, "PaidAmount cannot be negative");

		if (PaidAmount > TotalAmount)
			return (false, "PaidAmount cannot exceed TotalAmount");

		if (LedgerLines != null)
		{
			foreach (var line in LedgerLines)
			{
				var (isValid, errorMessage) = line.IsValid();
				if (!isValid)
					return (false, $"LedgerLine validation failed: {errorMessage}");
			}
		}

		return (true, null);
	}

	public Invoice ToModel()
	{
		return new Invoice
		{
			InvoiceId = InvoiceId,
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			OfficeName = OfficeName,
			InvoiceName = InvoiceName,
			ReservationId = ReservationId,
			ReservationCode = ReservationCode,
			InvoiceDate = InvoiceDate,
			DueDate = DueDate,
			TotalAmount = TotalAmount,
			PaidAmount = PaidAmount,
			Notes = Notes,
			IsActive = IsActive,
			LedgerLines = LedgerLines?.Select(l => l.ToModel()).ToList() ?? new List<LedgerLine>()
		};
	}
}

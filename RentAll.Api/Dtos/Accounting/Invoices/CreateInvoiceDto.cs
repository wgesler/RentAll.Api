using RentAll.Api.Dtos.Accounting.LedgerLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.Invoices;

public class CreateInvoiceDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; } = string.Empty;
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string? InvoicePeriod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public List<CreateLedgerLineDto> LedgerLines { get; set; } = new List<CreateLedgerLineDto>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(OfficeName))
            return (false, "OfficeName is required");

        if (string.IsNullOrWhiteSpace(InvoiceCode))
            return (false, "InvoiceCode is required");

        if (InvoiceDate == default)
            return (false, "InvoiceDate is required");

        if (TotalAmount < 0)
            return (false, "TotalAmount cannot be negative");

        if (PaidAmount < 0)
            return (false, "PaidAmount cannot be negative");

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

    public Invoice ToModel(Guid currentUser)
    {
        return new Invoice
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            OfficeName = OfficeName,
            InvoiceCode = InvoiceCode,
            ReservationId = ReservationId,
            ReservationCode = ReservationCode,
            InvoiceDate = InvoiceDate,
            DueDate = DueDate,
            InvoicePeriod = InvoicePeriod,
            TotalAmount = TotalAmount,
            PaidAmount = PaidAmount,
            Notes = Notes,
            IsActive = IsActive,
            LedgerLines = LedgerLines?.Select(l => l.ToModel(currentUser)).ToList() ?? new List<LedgerLine>(),
            CreatedBy = currentUser
        };
    }
}

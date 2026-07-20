namespace RentAll.Api.Dtos.Accounting.Invoices;

public class GetReservationInvoicePreviewsDto
{
    public Guid ReservationId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        return (true, null);
    }
}

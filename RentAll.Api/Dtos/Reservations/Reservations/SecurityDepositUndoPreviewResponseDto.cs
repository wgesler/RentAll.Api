namespace RentAll.Api.Dtos.Reservations.Reservations;

public class SecurityDepositUndoDocumentResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IncludesOtherReservations { get; set; }

    public SecurityDepositUndoDocumentResponseDto(SecurityDepositUndoDocument document)
    {
        Id = document.Id;
        Code = document.Code;
        Date = document.Date;
        Amount = document.Amount;
        Description = document.Description;
        IncludesOtherReservations = document.IncludesOtherReservations;
    }
}

public class SecurityDepositUndoPreviewResponseDto
{
    public string ReservationCode { get; set; } = string.Empty;
    public List<SecurityDepositUndoDocumentResponseDto> Transfers { get; set; } = [];
    public List<SecurityDepositUndoDocumentResponseDto> Deposits { get; set; } = [];
    public List<SecurityDepositUndoDocumentResponseDto> Payments { get; set; } = [];
    public List<SecurityDepositUndoDocumentResponseDto> Invoices { get; set; } = [];

    public SecurityDepositUndoPreviewResponseDto(SecurityDepositUndoPreview preview)
    {
        ReservationCode = preview.ReservationCode;
        Transfers = preview.Transfers.Select(document => new SecurityDepositUndoDocumentResponseDto(document)).ToList();
        Deposits = preview.Deposits.Select(document => new SecurityDepositUndoDocumentResponseDto(document)).ToList();
        Payments = preview.Payments.Select(document => new SecurityDepositUndoDocumentResponseDto(document)).ToList();
        Invoices = preview.Invoices.Select(document => new SecurityDepositUndoDocumentResponseDto(document)).ToList();
    }
}

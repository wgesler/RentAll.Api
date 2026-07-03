namespace RentAll.Api.Dtos.Logs;

public class GeneralErrorLogResponseDto
{
    public int Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public GeneralErrorLogResponseDto(LoggingErrorLog loggingErrorLog)
    {
        Id = loggingErrorLog.Id;
        OrganizationId = loggingErrorLog.OrganizationId;
        OfficeId = loggingErrorLog.OfficeId;
        ReservationId = loggingErrorLog.ReservationId;
        PropertyId = loggingErrorLog.PropertyId;
        InvoiceId = loggingErrorLog.InvoiceId;
        ReceiptId = loggingErrorLog.ReceiptId;
        JournalEntryId = loggingErrorLog.JournalEntryId;
        Message = loggingErrorLog.Message;
        Exception = loggingErrorLog.Exception;
        CreatedOn = loggingErrorLog.CreatedOn;
    }
}

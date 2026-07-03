namespace RentAll.Infrastructure.Entities.Logging;

public class GeneralErrorLogEntity
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
}

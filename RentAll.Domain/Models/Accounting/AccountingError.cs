namespace RentAll.Domain.Models;

public class AccountingError
{
    public Guid AccountingErrorId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? DocumentCode { get; set; }
    public DateOnly? AccountingPeriod { get; set; }
    public decimal? Amount { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
}

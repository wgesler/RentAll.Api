namespace RentAll.Api.Dtos.Logs;

public class AccountingErrorLogResponseDto
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

    public AccountingErrorLogResponseDto(AccountingError accountingError)
    {
        AccountingErrorId = accountingError.AccountingErrorId;
        OrganizationId = accountingError.OrganizationId;
        OfficeId = accountingError.OfficeId;
        Trigger = accountingError.Trigger;
        SourceTypeId = accountingError.SourceTypeId;
        SourceId = accountingError.SourceId;
        DocumentCode = accountingError.DocumentCode;
        AccountingPeriod = accountingError.AccountingPeriod;
        Amount = accountingError.Amount;
        Message = accountingError.Message;
        CreatedOn = accountingError.CreatedOn;
        CreatedBy = accountingError.CreatedBy;
    }
}

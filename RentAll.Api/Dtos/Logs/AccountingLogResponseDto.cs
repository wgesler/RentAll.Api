namespace RentAll.Api.Dtos.Logs;

public class AccountingLogResponseDto
{
    public int Id { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? RentalLine { get; set; }
    public bool Split { get; set; }
    public string? FirstPeriod { get; set; }
    public string? SecondPeriod { get; set; }
    public decimal? FirstAmount { get; set; }
    public decimal? SecondAmount { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public AccountingLogResponseDto(AccountingLog accountingLog)
    {
        Id = accountingLog.Id;
        OrganizationId = accountingLog.OrganizationId;
        OfficeId = accountingLog.OfficeId;
        PropertyId = accountingLog.PropertyId;
        InvoiceId = accountingLog.InvoiceId;
        OriginalAmount = accountingLog.OriginalAmount;
        RentalLine = accountingLog.RentalLine;
        Split = accountingLog.Split;
        FirstPeriod = accountingLog.FirstPeriod;
        SecondPeriod = accountingLog.SecondPeriod;
        FirstAmount = accountingLog.FirstAmount;
        SecondAmount = accountingLog.SecondAmount;
        Message = accountingLog.Message;
        CreatedOn = accountingLog.CreatedOn;
    }
}

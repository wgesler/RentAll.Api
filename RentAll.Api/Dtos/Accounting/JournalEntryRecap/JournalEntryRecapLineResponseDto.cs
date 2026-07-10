namespace RentAll.Api.Dtos.Accounting.JournalEntryRecap;

public class JournalEntryRecapLineResponseDto
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string SourceTypeCode { get; set; } = string.Empty;
    public string SourceDocumentCode { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string ChartOfAccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string RecapCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public JournalEntryRecapLineResponseDto(JournalEntryRecapLine line)
    {
        JournalEntryLineId = line.JournalEntryLineId;
        JournalEntryId = line.JournalEntryId;
        JournalEntryCode = line.JournalEntryCode;
        TransactionDate = line.TransactionDate;
        AccountingPeriod = line.AccountingPeriod;
        OfficeId = line.OfficeId;
        PropertyId = line.PropertyId;
        PropertyCode = line.PropertyCode;
        ReservationId = line.ReservationId;
        ReservationCode = line.ReservationCode;
        SourceTypeId = line.SourceTypeId;
        SourceId = line.SourceId;
        SourceTypeCode = line.SourceTypeCode;
        SourceDocumentCode = line.SourceDocumentCode;
        ChartOfAccountId = line.ChartOfAccountId;
        AccountNo = line.AccountNo;
        ChartOfAccountName = line.ChartOfAccountName;
        Description = line.Description;
        Debit = line.Debit;
        Credit = line.Credit;
        Activity = line.Activity;
        RecapCategory = line.RecapCategory;
        Amount = line.Amount;
    }
}

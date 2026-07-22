namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class JournalEntryLineSearchResponseDto
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public int ChartOfAccountId { get; set; }
    public int? CostCodeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
    public bool IsCleared { get; set; }
    public DateOnly? ClearedOn { get; set; }
    public int OfficeId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public int PostingStatusId { get; set; }
    public int JournalEntryKindId { get; set; }
    public int PerspectiveId { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceCode { get; set; }
    public string? CheckNumber { get; set; }
    public string? JournalEntryMemo { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTimeOffset JournalEntryCreatedOn { get; set; }

    public JournalEntryLineSearchResponseDto(JournalEntryLineSearchResult result)
    {
        JournalEntryLineId = result.JournalEntryLineId;
        JournalEntryId = result.JournalEntryId;
        ChartOfAccountId = result.ChartOfAccountId;
        CostCodeId = result.CostCodeId;
        PropertyId = result.PropertyId;
        PropertyCode = result.PropertyCode;
        ReservationId = result.ReservationId;
        ReservationCode = result.ReservationCode;
        ContactId = result.ContactId;
        ContactName = result.ContactName;
        Debit = result.Debit;
        Credit = result.Credit;
        Memo = result.Memo;
        IsCleared = result.IsCleared;
        ClearedOn = result.ClearedOn;
        OfficeId = result.OfficeId;
        JournalEntryCode = result.JournalEntryCode;
        TransactionDate = result.TransactionDate;
        AccountingPeriod = result.AccountingPeriod;
        PostingStatusId = result.PostingStatusId;
        JournalEntryKindId = result.JournalEntryKindId;
        PerspectiveId = result.PerspectiveId;
        SourceTypeId = result.SourceTypeId;
        SourceId = result.SourceId;
        SourceCode = result.SourceCode;
        CheckNumber = result.CheckNumber;
        JournalEntryMemo = result.JournalEntryMemo;
        CreatedOn = result.CreatedOn;
        CreatedBy = result.CreatedBy;
        ModifiedOn = result.ModifiedOn;
        ModifiedBy = result.ModifiedBy;
        JournalEntryCreatedOn = result.JournalEntryCreatedOn;
    }
}

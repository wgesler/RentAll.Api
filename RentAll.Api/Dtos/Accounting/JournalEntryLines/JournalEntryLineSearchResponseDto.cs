using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class JournalEntryLineSearchResponseDto
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public int ChartOfAccountId { get; set; }
    public int? CostCodeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? ContactId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
    public int OfficeId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly PostingDate { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public int? SourceReceiptId { get; set; }
    public int? SourcePaymentSequence { get; set; }
    public string? JournalEntryMemo { get; set; }
    public bool IsPosted { get; set; }
    public bool IsVoided { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public JournalEntryLineSearchResponseDto(JournalEntryLineSearchResult result)
    {
        JournalEntryLineId = result.JournalEntryLineId;
        JournalEntryId = result.JournalEntryId;
        ChartOfAccountId = result.ChartOfAccountId;
        CostCodeId = result.CostCodeId;
        PropertyId = result.PropertyId;
        ReservationId = result.ReservationId;
        ContactId = result.ContactId;
        Debit = result.Debit;
        Credit = result.Credit;
        Memo = result.Memo;
        OfficeId = result.OfficeId;
        JournalEntryCode = result.JournalEntryCode;
        TransactionDate = result.TransactionDate;
        PostingDate = result.PostingDate;
        SourceTypeId = result.SourceTypeId;
        SourceId = result.SourceId;
        SourceReceiptId = result.SourceReceiptId;
        SourcePaymentSequence = result.SourcePaymentSequence;
        JournalEntryMemo = result.JournalEntryMemo;
        IsPosted = result.IsPosted;
        IsVoided = result.IsVoided;
        CreatedOn = result.CreatedOn;
        CreatedBy = result.CreatedBy;
        ModifiedOn = result.ModifiedOn;
        ModifiedBy = result.ModifiedBy;
    }
}

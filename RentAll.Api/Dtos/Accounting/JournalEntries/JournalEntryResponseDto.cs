using RentAll.Api.Dtos.Accounting.JournalEntryLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class JournalEntryResponseDto
{
    public Guid JournalEntryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public int PostingStatusId { get; set; }
    public int? SourceTypeId { get; set; }
    public int JournalEntryKindId { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceCode { get; set; }
    public string? CheckNumber { get; set; }
    public string? Memo { get; set; }
    public bool IsCashOnly { get; set; } = false;
    public List<JournalEntryLineResponseDto> JournalEntryLines { get; set; } = new List<JournalEntryLineResponseDto>();
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public JournalEntryResponseDto(JournalEntry journalEntry)
    {
        JournalEntryId = journalEntry.JournalEntryId;
        OrganizationId = journalEntry.OrganizationId;
        OfficeId = journalEntry.OfficeId;
        JournalEntryCode = journalEntry.JournalEntryCode;
        TransactionDate = journalEntry.TransactionDate;
        AccountingPeriod = journalEntry.AccountingPeriod;
        PostingStatusId = (int)journalEntry.PostingStatusId;
        SourceTypeId = journalEntry.SourceTypeId;
        JournalEntryKindId = (int)journalEntry.JournalEntryKindId;
        SourceId = journalEntry.SourceId;
        SourceCode = journalEntry.SourceCode;
        CheckNumber = journalEntry.CheckNumber;
        Memo = journalEntry.Memo;
        IsCashOnly = journalEntry.IsCashOnly;
        JournalEntryLines = journalEntry.JournalEntryLines.Select(l => new JournalEntryLineResponseDto(l)).ToList();
        CreatedOn = journalEntry.CreatedOn;
        CreatedBy = journalEntry.CreatedBy;
        ModifiedOn = journalEntry.ModifiedOn;
        ModifiedBy = journalEntry.ModifiedBy;
    }
}

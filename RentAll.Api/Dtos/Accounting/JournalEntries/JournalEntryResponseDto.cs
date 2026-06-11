using RentAll.Api.Dtos.Accounting.JournalEntryLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class JournalEntryResponseDto
{
    public Guid JournalEntryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly PostingDate { get; set; }
    public int TransactionTypeId { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? Memo { get; set; }
    public bool IsPosted { get; set; }
    public bool IsVoided { get; set; }
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
        TransactionDate = journalEntry.TransactionDate;
        PostingDate = journalEntry.PostingDate;
        TransactionTypeId = journalEntry.TransactionTypeId;
        SourceTypeId = journalEntry.SourceTypeId;
        SourceId = journalEntry.SourceId;
        Memo = journalEntry.Memo;
        IsPosted = journalEntry.IsPosted;
        IsVoided = journalEntry.IsVoided;
        JournalEntryLines = journalEntry.JournalEntryLines.Select(l => new JournalEntryLineResponseDto(l)).ToList();
        CreatedOn = journalEntry.CreatedOn;
        CreatedBy = journalEntry.CreatedBy;
        ModifiedOn = journalEntry.ModifiedOn;
        ModifiedBy = journalEntry.ModifiedBy;
    }
}

using RentAll.Api.Dtos.Accounting.JournalEntryLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class CreateJournalEntryDto
{
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
    public List<CreateJournalEntryLineDto> JournalEntryLines { get; set; } = new List<CreateJournalEntryLineDto>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (TransactionDate == default)
            return (false, "TransactionDate is required");

        if (PostingDate == default)
            return (false, "PostingDate is required");

        if (JournalEntryLines != null)
        {
            foreach (var line in JournalEntryLines)
            {
                var (isValid, errorMessage) = line.IsValid();
                if (!isValid)
                    return (false, $"JournalEntryLine validation failed: {errorMessage}");
            }
        }

        return (true, null);
    }

    public JournalEntry ToModel(Guid currentUser)
    {
        return new JournalEntry
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TransactionDate = TransactionDate,
            PostingDate = PostingDate,
            TransactionTypeId = TransactionTypeId,
            SourceTypeId = SourceTypeId,
            SourceId = SourceId,
            Memo = Memo,
            IsPosted = IsPosted,
            IsVoided = IsVoided,
            JournalEntryLines = JournalEntryLines?.Select(l => l.ToModel(currentUser)).ToList() ?? new List<JournalEntryLine>(),
            CreatedBy = currentUser
        };
    }
}

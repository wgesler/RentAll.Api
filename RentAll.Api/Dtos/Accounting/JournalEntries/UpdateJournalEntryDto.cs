using RentAll.Api.Dtos.Accounting.JournalEntryLines;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class UpdateJournalEntryDto
{
    public Guid JournalEntryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public int PostingStatusId { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? Memo { get; set; }
    public List<UpdateJournalEntryLineDto> JournalEntryLines { get; set; } = new List<UpdateJournalEntryLineDto>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (JournalEntryId == Guid.Empty)
            return (false, "JournalEntryId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (TransactionDate == default)
            return (false, "TransactionDate is required");

        if (AccountingPeriod == default)
            return (false, "AccountingPeriod is required");

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
            JournalEntryId = JournalEntryId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TransactionDate = TransactionDate,
            AccountingPeriod = AccountingPeriod,
            PostingStatusId = (PostingStatus)PostingStatusId,
            SourceTypeId = SourceTypeId,
            SourceId = SourceId,
            Memo = Memo,
            JournalEntryLines = JournalEntryLines?.Select(l => l.ToModel(currentUser)).ToList() ?? new List<JournalEntryLine>(),
            ModifiedBy = currentUser,
            CreatedBy = currentUser
        };
    }
}

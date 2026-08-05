using RentAll.Api.Dtos.Accounting.JournalEntryLines;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class CreateJournalEntryDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public int PostingStatusId { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceCode { get; set; }
    public string? Memo { get; set; }
    public bool IsCashOnly { get; set; } = false;
    public Guid? PaymentId { get; set; }
    public string? PaymentCode { get; set; }
    public Guid? DepositId { get; set; }
    public string? DepositCode { get; set; }
    public Guid? TransferId { get; set; }
    public string? TransferCode { get; set; }
    public int JournalEntryKindId { get; set; } = (int)JournalEntryKind.Manual;
    public List<CreateJournalEntryLineDto> JournalEntryLines { get; set; } = new List<CreateJournalEntryLineDto>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TransactionDate = TransactionDate,
            AccountingPeriod = AccountingPeriod,
            PostingStatusId = (PostingStatus)PostingStatusId,
            SourceTypeId = SourceTypeId,
            SourceId = SourceId,
            SourceCode = SourceCode,
            Memo = Memo,
            IsCashOnly = IsCashOnly,
            PaymentId = PaymentId is { } paymentId && paymentId != Guid.Empty ? paymentId : null,
            PaymentCode = PaymentCode,
            DepositId = DepositId is { } depositId && depositId != Guid.Empty ? depositId : null,
            DepositCode = DepositCode,
            TransferId = TransferId is { } transferId && transferId != Guid.Empty ? transferId : null,
            TransferCode = TransferCode,
            JournalEntryKindId = (JournalEntryKind)JournalEntryKindId,
            JournalEntryLines = JournalEntryLines?.Select(l => l.ToModel(currentUser)).ToList() ?? new List<JournalEntryLine>(),
            CreatedBy = currentUser
        };
    }
}

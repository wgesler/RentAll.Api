using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class UpdateJournalEntryLineDto
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (JournalEntryId == Guid.Empty)
            return (false, "JournalEntryId is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (Debit < 0 || Credit < 0)
            return (false, "Debit and Credit must be zero or greater");

        return (true, null);
    }

    public JournalEntryLine ToModel(Guid currentUser)
    {
        return new JournalEntryLine
        {
            JournalEntryLineId = JournalEntryLineId,
            JournalEntryId = JournalEntryId,
            ChartOfAccountId = ChartOfAccountId,
            CostCodeId = CostCodeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            ContactId = ContactId,
            Debit = Debit,
            Credit = Credit,
            Memo = string.IsNullOrWhiteSpace(Memo) ? null : Memo.Trim(),
            ModifiedBy = currentUser
        };
    }
}

namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class CreateJournalEntryLineDto
{
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
        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (Debit < 0 || Credit < 0)
            return (false, "Debit and Credit must be zero or greater");

        if (Debit == 0 && Credit == 0)
            return (false, "Debit or Credit is required");

        return (true, null);
    }

    public JournalEntryLine ToModel(Guid currentUser)
    {
        return new JournalEntryLine
        {
            JournalEntryId = JournalEntryId,
            ChartOfAccountId = ChartOfAccountId,
            CostCodeId = CostCodeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            ContactId = ContactId,
            Debit = Debit,
            Credit = Credit,
            Memo = string.IsNullOrWhiteSpace(Memo) ? null : Memo.Trim(),
            CreatedBy = currentUser
        };
    }
}

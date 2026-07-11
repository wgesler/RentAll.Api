namespace RentAll.Api.Dtos.Accounting.Deposits;

public class DepositSplitDto
{
    public int? DepositSplitId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public int? ChartOfAccountId { get; set; }
    public string? ChartOfAccountDisplayName { get; set; }

    public DepositSplitDto()
    {
    }

    public DepositSplitDto(DepositSplit split)
    {
        DepositSplitId = split.DepositSplitId;
        Amount = split.Amount;
        Description = split.Description;
        PropertyId = split.PropertyId;
        PropertyCode = split.PropertyCode;
        ReservationId = split.ReservationId;
        ReservationCode = split.ReservationCode;
        ContactId = split.ContactId;
        ContactName = split.ContactName;
        JournalEntryLineId = split.JournalEntryLineId;
        ChartOfAccountId = split.ChartOfAccountId;
        ChartOfAccountDisplayName = split.ChartOfAccountDisplayName;
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ChartOfAccountId is null or <= 0)
            return (false, "ChartOfAccountId is required");

        return (true, null);
    }

    public DepositSplit ToModel()
    {
        return new DepositSplit
        {
            DepositSplitId = DepositSplitId ?? 0,
            Amount = Amount,
            Description = Description,
            PropertyId = PropertyId == Guid.Empty ? null : PropertyId,
            ReservationId = ReservationId == Guid.Empty ? null : ReservationId,
            ContactId = ContactId == Guid.Empty ? null : ContactId,
            JournalEntryLineId = JournalEntryLineId == Guid.Empty ? null : JournalEntryLineId,
            ChartOfAccountId = ChartOfAccountId is > 0 ? ChartOfAccountId : null
        };
    }
}

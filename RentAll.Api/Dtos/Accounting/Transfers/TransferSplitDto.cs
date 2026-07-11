namespace RentAll.Api.Dtos.Accounting.Transfers;

public class TransferSplitDto
{
    public int? TransferSplitId { get; set; }
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

    public TransferSplitDto()
    {
    }

    public TransferSplitDto(TransferSplit split)
    {
        TransferSplitId = split.TransferSplitId;
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

    public TransferSplit ToModel()
    {
        return new TransferSplit
        {
            TransferSplitId = TransferSplitId ?? 0,
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

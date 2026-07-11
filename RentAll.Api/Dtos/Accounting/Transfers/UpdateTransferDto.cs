namespace RentAll.Api.Dtos.Accounting.Transfers;

public class UpdateTransferDto
{
    public Guid TransferId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly TransferDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public int? BankAccountId { get; set; }
    public List<TransferSplitDto> Splits { get; set; } = new List<TransferSplitDto>();
    public Guid? JournalEntryId { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TransferId == Guid.Empty)
            return (false, "TransferId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (TransferDate == default)
            return (false, "TransferDate is required");

        if (AccountingPeriod == default)
            return (false, "AccountingPeriod is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (BankAccountId is null or <= 0)
            return (false, "BankAccountId is required");

        if (Splits == null || Splits.Count == 0)
            return (false, "At least one split is required");

        foreach (var split in Splits)
        {
            var (isValid, errorMessage) = split.IsValid();
            if (!isValid)
                return (false, $"Split validation failed: {errorMessage}");
        }

        return (true, null);
    }

    public Transfer ToModel(Guid currentUser)
    {
        return new Transfer
        {
            TransferId = TransferId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TransferDate = TransferDate,
            AccountingPeriod = AccountingPeriod,
            Description = Description,
            Amount = Amount,
            PropertyId = PropertyId == Guid.Empty ? null : PropertyId,
            BankAccountId = BankAccountId is > 0 ? BankAccountId : null,
            Splits = Splits.Select(split => split.ToModel()).ToList(),
            JournalEntryId = JournalEntryId,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}

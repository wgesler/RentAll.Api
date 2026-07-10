namespace RentAll.Api.Dtos.Accounting.BankDeposits;

public class CreateDepositDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly DepositDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public int? BankAccountId { get; set; }
    public List<DepositSplitDto> Splits { get; set; } = new List<DepositSplitDto>();
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (DepositDate == default)
            return (false, "DepositDate is required");

        if (AccountingPeriod == default)
            return (false, "AccountingPeriod is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (BankAccountId is null or <= 0)
            return (false, "BankAccountId is required");

        if (Splits == null || Splits.Count == 0)
            return (false, "At least one split is required");

        if (!Splits.Any(split => split.PropertyId.HasValue && split.PropertyId != Guid.Empty))
            return (false, "At least one split must have a PropertyId");

        foreach (var split in Splits)
        {
            var (isValid, errorMessage) = split.IsValid();
            if (!isValid)
                return (false, $"Split validation failed: {errorMessage}");
        }

        return (true, null);
    }

    public Deposit ToModel(string depositCode, Guid currentUser)
    {
        return new Deposit
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            DepositCode = depositCode.Trim(),
            DepositDate = DepositDate,
            AccountingPeriod = AccountingPeriod,
            Amount = Amount,
            Description = Description,
            PropertyId = PropertyId == Guid.Empty ? null : PropertyId,
            BankAccountId = BankAccountId is > 0 ? BankAccountId : null,
            Splits = Splits.Select(split => split.ToModel()).ToList(),
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

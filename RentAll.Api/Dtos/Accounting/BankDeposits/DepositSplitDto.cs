namespace RentAll.Api.Dtos.Accounting.BankDeposits;

public class DepositSplitDto
{
    public int? DepositSplitId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
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
        ChartOfAccountId = split.ChartOfAccountId;
        ChartOfAccountDisplayName = split.ChartOfAccountDisplayName;
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ChartOfAccountId.HasValue && ChartOfAccountId.Value <= 0)
            return (false, "ChartOfAccountId must be greater than 0 when provided");

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
            ChartOfAccountId = ChartOfAccountId is > 0 ? ChartOfAccountId : null
        };
    }
}

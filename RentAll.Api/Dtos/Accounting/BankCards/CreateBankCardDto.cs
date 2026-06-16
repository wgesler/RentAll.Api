namespace RentAll.Api.Dtos.Accounting.BankCards;

public class CreateBankCardDto
{
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!Enum.IsDefined(typeof(CardType), CardTypeId))
            return (false, $"Invalid CardTypeId value: {CardTypeId}");

        if (string.IsNullOrWhiteSpace(CardName))
            return (false, "CardName is required");

        if (string.IsNullOrWhiteSpace(CardNumber))
            return (false, "CardNumber is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        return (true, null);
    }

    public BankCard ToModel(Guid organizationId, int officeId)
    {
        return new BankCard
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            CardTypeId = CardTypeId,
            CardName = CardName,
            CardNumber = CardNumber,
            ChartOfAccountId = ChartOfAccountId
        };
    }
}

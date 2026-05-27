namespace RentAll.Api.Dtos.Accounting.BankCards;

public class UpdateBankCardDto
{
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public int CostCodeId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!Enum.IsDefined(typeof(CardType), CardTypeId))
            return (false, $"Invalid CardTypeId value: {CardTypeId}");

        if (string.IsNullOrWhiteSpace(CardName))
            return (false, "CardName is required");

        if (string.IsNullOrWhiteSpace(CardNumber))
            return (false, "CardNumber is required");

        if (CostCodeId <= 0)
            return (false, "CostCodeId is required");

        return (true, null);
    }

    public BankCard ToModel(int bankCardId, Guid organizationId, int officeId)
    {
        return new BankCard
        {
            BankCardId = bankCardId,
            OrganizationId = organizationId,
            OfficeId = officeId,
            CardTypeId = CardTypeId,
            CardName = CardName,
            CardNumber = CardNumber,
            CostCodeId = CostCodeId
        };
    }
}

namespace RentAll.Api.Dtos.Contacts.ContactCards;

public class UpsertContactCardDto
{
    public int? ContactCardId { get; set; }
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;

    public bool IsEmpty() => string.IsNullOrWhiteSpace(CardName) && string.IsNullOrWhiteSpace(CardNumber);

    public (bool IsValid, string? ErrorMessage) IsValid(bool requireCardNumber)
    {
        if (IsEmpty())
            return (true, null);

        if (!Enum.IsDefined(typeof(CardType), CardTypeId))
            return (false, $"Invalid CardTypeId value: {CardTypeId}");

        if (string.IsNullOrWhiteSpace(CardName))
            return (false, "CardName is required");

        if (requireCardNumber && string.IsNullOrWhiteSpace(CardNumber))
            return (false, "CardNumber is required");

        return (true, null);
    }
}

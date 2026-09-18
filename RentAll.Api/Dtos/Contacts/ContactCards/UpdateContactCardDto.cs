namespace RentAll.Api.Dtos.Contacts.ContactCards;

public class UpdateContactCardDto
{
    public int ContactCardId { get; set; }
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ContactCardId <= 0)
            return (false, "ContactCardId is required");

        if (!Enum.IsDefined(typeof(CardType), CardTypeId))
            return (false, $"Invalid CardTypeId value: {CardTypeId}");

        if (string.IsNullOrWhiteSpace(CardName))
            return (false, "CardName is required");

        return (true, null);
    }

    public ContactCard ToModel(Guid organizationId, int officeId)
    {
        return new ContactCard
        {
            ContactCardId = ContactCardId,
            OrganizationId = organizationId,
            OfficeId = officeId,
            CardTypeId = CardTypeId,
            CardName = CardName,
            CardNumber = CardNumber
        };
    }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Contacts.ContactCards;

public class ContactCardResponseDto
{
    public int ContactCardId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;

    public ContactCardResponseDto(ContactCard card)
    {
        ContactCardId = card.ContactCardId;
        OrganizationId = card.OrganizationId;
        OfficeId = card.OfficeId;
        CardTypeId = card.CardTypeId;
        CardName = card.CardName;
        DisplayName = card.DisplayName;
        LastFour = card.LastFour;
    }
}

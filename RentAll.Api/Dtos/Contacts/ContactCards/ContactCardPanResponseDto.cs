namespace RentAll.Api.Dtos.Contacts.ContactCards;

public class ContactCardPanResponseDto
{
    public string CardNumber { get; set; } = string.Empty;

    public ContactCardPanResponseDto(string cardNumber)
    {
        CardNumber = cardNumber;
    }
}

namespace RentAll.Api.Dtos.Accounting.BankCards;

public class BankCardPanResponseDto
{
    public string CardNumber { get; set; } = string.Empty;

    public BankCardPanResponseDto(string cardNumber)
    {
        CardNumber = cardNumber;
    }
}

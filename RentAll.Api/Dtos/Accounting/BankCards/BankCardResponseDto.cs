namespace RentAll.Api.Dtos.Accounting.BankCards;

public class BankCardResponseDto
{
    public int BankCardId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }

    public BankCardResponseDto(BankCard bankCard)
    {
        BankCardId = bankCard.BankCardId;
        OrganizationId = bankCard.OrganizationId;
        OfficeId = bankCard.OfficeId;
        CardTypeId = bankCard.CardTypeId;
        CardName = bankCard.CardName;
        DisplayName = bankCard.DisplayName;
        CardNumber = bankCard.CardNumber;
        LastFour = bankCard.LastFour;
        ChartOfAccountId = bankCard.ChartOfAccountId;
    }
}

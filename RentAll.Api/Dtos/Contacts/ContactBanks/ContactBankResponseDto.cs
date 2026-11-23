namespace RentAll.Api.Dtos.Contacts.ContactBanks;

public class ContactBankResponseDto
{
    public int ContactBankId { get; set; }
    public int BankAccountTypeId { get; set; }
    public string? NameOnAccount { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? LastFour { get; set; }
    public bool HasNumberConsent { get; set; }
    public DateTimeOffset? AuthorizedOn { get; set; }

    public ContactBankResponseDto(ContactBank bank)
    {
        ContactBankId = bank.ContactBankId;
        BankAccountTypeId = bank.BankAccountTypeId;
        NameOnAccount = bank.NameOnAccount;
        BankName = bank.BankName;
        RoutingNumber = bank.RoutingNumber;
        LastFour = bank.LastFour;
        HasNumberConsent = bank.NumberConsentId is > 0;
        AuthorizedOn = bank.AuthorizedOn;
    }
}

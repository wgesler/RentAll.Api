namespace RentAll.Api.Dtos.Contacts.ContactBanks;

/// <summary>The contact's bank account from the contact screen. A blank or masked AccountNumber keeps the stored one.</summary>
public class UpsertContactBankDto
{
    public int? ContactBankId { get; set; }
    public int BankAccountTypeId { get; set; }
    public string? NameOnAccount { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }

    public bool IsEmpty() => BankAccountTypeId == (int)BankAccountType.Unknown && string.IsNullOrWhiteSpace(NameOnAccount) && string.IsNullOrWhiteSpace(BankName) && string.IsNullOrWhiteSpace(RoutingNumber) && string.IsNullOrWhiteSpace(AccountNumber);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (IsEmpty())
            return (true, null);

        if (!Enum.IsDefined(typeof(BankAccountType), BankAccountTypeId))
            return (false, $"Invalid BankAccountTypeId value: {BankAccountTypeId}");
        if ((NameOnAccount ?? string.Empty).Trim().Length > 100)
            return (false, "NameOnAccount must be 100 characters or fewer");
        if ((BankName ?? string.Empty).Trim().Length > 250)
            return (false, "BankName must be 250 characters or fewer");
        if ((RoutingNumber ?? string.Empty).Trim().Length > 50)
            return (false, "RoutingNumber must be 50 characters or fewer");

        return (true, null);
    }

    public ContactBank ToModel()
    {
        return new ContactBank
        {
            ContactBankId = ContactBankId ?? 0,
            BankAccountTypeId = BankAccountTypeId,
            NameOnAccount = TrimOrNull(NameOnAccount),
            BankName = TrimOrNull(BankName),
            RoutingNumber = TrimOrNull(RoutingNumber)
        };
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

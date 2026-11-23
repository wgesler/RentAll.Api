using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>
/// A tenant's bank account from the pay-request page's ACH Debit form. The numbers are passed to Number and saved as the
/// tenant contact's bank account (ContactBank, account number encrypted); they are never logged.
/// </summary>
public class PayRequestAchRequestDto
{
    /// <summary>From the route; set by the controller.</summary>
    public string Token { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string ConfirmAccountNumber { get; set; } = string.Empty;
    /// <summary>1 Personal Checking, 2 Personal Savings, 3 Business Checking, 4 Business Savings.</summary>
    public int AccountType { get; set; }
    public string NameOnAccount { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    /// <summary>The tenant ticked the ACH authorization on the page.</summary>
    public bool Authorized { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var (tokenValid, tokenError) = PayRequestRequestDto.ValidateToken(Token);
        if (!tokenValid)
            return (false, tokenError);

        var routing = Digits(RoutingNumber);
        if (routing.Length != 9 || !IsValidRoutingChecksum(routing))
            return (false, "Enter a valid 9-digit routing number.");

        var account = Digits(AccountNumber);
        if (account.Length < 4 || account.Length > 17 || account.Length != (AccountNumber ?? string.Empty).Trim().Length)
            return (false, "Enter a valid account number (4 to 17 digits).");
        if (account != Digits(ConfirmAccountNumber))
            return (false, "The account numbers don't match.");

        if (!Enum.IsDefined(typeof(NumberAchAccountType), AccountType))
            return (false, "Choose the account type.");

        var name = (NameOnAccount ?? string.Empty).Trim();
        if (name.Length < 2 || name.Length > 100)
            return (false, "Enter the name on the account.");

        var bankName = (BankName ?? string.Empty).Trim();
        if (bankName.Length < 2 || bankName.Length > 250)
            return (false, "Enter the bank name.");

        if (!Authorized)
            return (false, "Please accept the ACH authorization.");

        return (true, null);
    }

    /// <summary>The account to save with Number; the controller adds the reservation and payer details.</summary>
    public NumberAchConsentRequest ToModel()
    {
        var name = (NameOnAccount ?? string.Empty).Trim();
        var space = name.LastIndexOf(' ');
        return new NumberAchConsentRequest
        {
            RoutingNumber = Digits(RoutingNumber),
            AccountNumber = Digits(AccountNumber),
            AccountType = (NumberAchAccountType)AccountType,
            FirstName = space > 0 ? name[..space].Trim() : name,
            LastName = space > 0 ? name[(space + 1)..].Trim() : string.Empty
        };
    }

    public string AccountDigits => Digits(AccountNumber);
    public string RoutingDigits => Digits(RoutingNumber);
    public string AccountLast4 => Last4(Digits(AccountNumber));

    /// <summary>
    /// US routing (ABA) numbers carry a check digit: 3*(d1+d4+d7) + 7*(d2+d5+d8) + (d3+d6+d9) must be a multiple of 10.
    /// </summary>
    public static bool IsValidRoutingChecksum(string routing)
    {
        if (routing.Length != 9 || !routing.All(char.IsDigit))
            return false;
        int D(int i) => routing[i] - '0';
        var sum = 3 * (D(0) + D(3) + D(6)) + 7 * (D(1) + D(4) + D(7)) + (D(2) + D(5) + D(8));
        return sum > 0 && sum % 10 == 0;
    }

    private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
    private static string Last4(string digits) => digits.Length <= 4 ? digits : digits[^4..];
}

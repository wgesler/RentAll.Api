namespace RentAll.Domain.Enums;

/// <summary>Matches Accounting.BankAccountType. 1-4 are EasyPay's (Number) ACH account types; 0 is never sent to EasyPay.</summary>
public enum BankAccountType
{
    Unknown = 0,
    PersonalChecking = 1,
    PersonalSavings = 2,
    BusinessChecking = 3,
    BusinessSavings = 4
}

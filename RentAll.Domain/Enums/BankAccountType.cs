namespace RentAll.Domain.Enums;

/// <summary>Matches Accounting.BankAccountType. 1-4 are Number's ACH account types; 0 is never sent to Number.</summary>
public enum BankAccountType
{
    Unknown = 0,
    PersonalChecking = 1,
    PersonalSavings = 2,
    BusinessChecking = 3,
    BusinessSavings = 4
}

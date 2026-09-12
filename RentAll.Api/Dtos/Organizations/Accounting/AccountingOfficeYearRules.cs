namespace RentAll.Api.Dtos.Organizations.Accounting;

public static class AccountingOfficeYearRules
{
    public const int MinExclusiveYear = 2020;

    public static int MaxYear => DateTime.UtcNow.Year;

    public static (bool IsValid, string? ErrorMessage) ValidateYear(int year, string fieldName)
    {
        if (year <= MinExclusiveYear)
            return (false, $"{fieldName} must be after {MinExclusiveYear}.");

        if (year > MaxYear)
            return (false, $"{fieldName} must be no later than {MaxYear}.");

        return (true, null);
    }
}

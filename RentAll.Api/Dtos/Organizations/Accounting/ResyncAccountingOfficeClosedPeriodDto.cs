namespace RentAll.Api.Dtos.Organizations.Accounting;

public class ResyncAccountingOfficeClosedPeriodDto
{
    public Guid OrganizationId { get; set; }
    public int SoftClosedMonth { get; set; }
    public int SoftClosedYear { get; set; }
    public int HardClosedMonth { get; set; }
    public int HardClosedYear { get; set; }
    public int StartMonth { get; set; }
    public int StartYear { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (SoftClosedMonth < 1 || SoftClosedMonth > 12)
            return (false, "SoftClosedMonth must be between 1 and 12.");

        var softClosedYearCheck = AccountingOfficeYearRules.ValidateYear(SoftClosedYear, nameof(SoftClosedYear));
        if (!softClosedYearCheck.IsValid)
            return softClosedYearCheck;

        if (HardClosedMonth < 1 || HardClosedMonth > 12)
            return (false, "HardClosedMonth must be between 1 and 12.");

        var hardClosedYearCheck = AccountingOfficeYearRules.ValidateYear(HardClosedYear, nameof(HardClosedYear));
        if (!hardClosedYearCheck.IsValid)
            return hardClosedYearCheck;

        if (StartMonth < 1 || StartMonth > 12)
            return (false, "StartMonth must be between 1 and 12.");

        var startYearCheck = AccountingOfficeYearRules.ValidateYear(StartYear, nameof(StartYear));
        if (!startYearCheck.IsValid)
            return startYearCheck;

        return (true, null);
    }
}

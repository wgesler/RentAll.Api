namespace RentAll.Api.Dtos.Organizations.Accounting;

public class ReopenHardClosedPostingStatusDto
{
    public Guid OrganizationId { get; set; }
    public int HardClosedMonth { get; set; }
    public int HardClosedYear { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (HardClosedMonth < 1 || HardClosedMonth > 12)
            return (false, "HardClosedMonth must be between 1 and 12.");

        return AccountingOfficeYearRules.ValidateYear(HardClosedYear, nameof(HardClosedYear));
    }
}

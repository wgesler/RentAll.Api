using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class CreditReportRequestDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "Credit report file is required");

        return (true, null);
    }
}

public class CreditReportCreateDraftsRequestDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public List<CreditReportLineDto> Lines { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        return (true, null);
    }
}

public class CreditReportMatchDto
{
    public string? SourceName { get; set; }
    public Guid? MatchedId { get; set; }
    public string? MatchedName { get; set; }
}

public class CreditReportSaveMatchesRequestDto
{
    public Guid OrganizationId { get; set; }
    public List<CreditReportMatchDto> Matches { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (Matches == null || Matches.Count == 0)
            return (false, "At least one match is required");

        return (true, null);
    }
}

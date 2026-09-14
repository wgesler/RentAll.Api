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

        if (Lines == null || Lines.Count == 0)
            return (false, "At least one credit report line is required");

        return (true, null);
    }
}

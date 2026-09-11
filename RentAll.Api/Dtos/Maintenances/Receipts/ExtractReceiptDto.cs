using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ExtractReceiptDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "Receipt file is required");

        return (true, null);
    }
}

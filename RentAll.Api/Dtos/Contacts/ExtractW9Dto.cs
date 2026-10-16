using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Contacts;

public class ExtractW9Dto
{
    public Guid OrganizationId { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (FileDetails == null || (string.IsNullOrWhiteSpace(FileDetails.File) && string.IsNullOrWhiteSpace(FileDetails.DataUrl)))
            return (false, "W-9 file is required");

        return (true, null);
    }
}

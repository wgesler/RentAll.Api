using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Photos;

public class CreatePhotoDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid MaintenanceId { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (MaintenanceId == Guid.Empty)
            return (false, "MaintenanceId is required");

        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "File is required");

        if (string.IsNullOrWhiteSpace(FileDetails.FileName))
            return (false, "File name is required");

        if (string.IsNullOrWhiteSpace(FileDetails.ContentType))
            return (false, "Content type is required");

        return (true, null);
    }

    public Photo ToModel(Guid organizationId, Guid currentUser)
    {
        return new Photo
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            MaintenanceId = MaintenanceId,
            PhotoPath = null, // Will be set by controller after file save
            CreatedBy = currentUser
        };
    }
}

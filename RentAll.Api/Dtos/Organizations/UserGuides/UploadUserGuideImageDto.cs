using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.UserGuides;

public class UploadUserGuideImageDto
{
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "Image file is required");

        if (string.IsNullOrWhiteSpace(FileDetails.FileName))
            return (false, "File name is required");

        if (string.IsNullOrWhiteSpace(FileDetails.ContentType))
            return (false, "Content type is required");

        return (true, null);
    }
}

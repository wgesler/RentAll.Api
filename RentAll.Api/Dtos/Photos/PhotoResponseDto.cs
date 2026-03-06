using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Photos;

public class PhotoResponseDto
{
    public Guid PhotoId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? PhotoPath { get; set; } = string.Empty;
    public FileDetails? FileDetails { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }

    public PhotoResponseDto(Photo photo)
    {
        PhotoId = photo.PhotoId;
        OrganizationId = photo.OrganizationId;
        OfficeId = photo.OfficeId;
        MaintenanceId = photo.MaintenanceId;
        FileName = photo.FileName;
        FileExtension = photo.FileExtension;
        ContentType = photo.ContentType;
        PhotoPath = photo.PhotoPath;
        CreatedOn = photo.CreatedOn;
        CreatedBy = photo.CreatedBy;
    }
}

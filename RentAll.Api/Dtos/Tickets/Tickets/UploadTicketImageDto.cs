using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Tickets.Tickets;

public class UploadTicketImageDto
{
    public int OfficeId { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "Image file is required");

        if (string.IsNullOrWhiteSpace(FileDetails.FileName))
            return (false, "File name is required");

        if (string.IsNullOrWhiteSpace(FileDetails.ContentType))
            return (false, "Content type is required");

        return (true, null);
    }
}

public class TicketImageUploadResponseDto
{
    public string ImagePath { get; set; } = string.Empty;
}

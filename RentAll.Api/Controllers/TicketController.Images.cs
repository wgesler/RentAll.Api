using System.Text.RegularExpressions;
using RentAll.Api.Dtos.Tickets.Tickets;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers;

public partial class TicketController
{
    private static readonly Regex TicketImagePathRegex = new(
        "data-ticket-image-path=\"(?<path>[^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TicketImageDataSrcRegex = new(
        "\\ssrc=\"data:[^\"]*\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [HttpPost("image")]
    public async Task<IActionResult> UploadTicketImage([FromBody] UploadTicketImageDto dto)
    {
        if (dto == null)
            return BadRequest("Image data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, CurrentOrganizationId);
            if (office == null)
                return BadRequest("Office not found");

            var imagePath = await _fileAttachmentHelper.SaveImageIfPresentAsync(
                CurrentOrganizationId,
                office.Name,
                dto.FileDetails,
                ImageType.TicketImage);

            if (string.IsNullOrWhiteSpace(imagePath))
                return BadRequest("Unable to save ticket image");

            return Ok(new TicketImageUploadResponseDto { ImagePath = imagePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading ticket image");
            return ServerError("An error occurred while uploading the ticket image");
        }
    }

    [HttpGet("image")]
    public async Task<IActionResult> GetTicketImage([FromQuery] string path, [FromQuery] int officeId)
    {
        if (string.IsNullOrWhiteSpace(path) || officeId <= 0)
            return BadRequest("Image path and office are required");

        if (!path.Contains("/ticketimage/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Invalid ticket image path");

        try
        {
            var office = await _organizationRepository.GetOfficeByIdAsync(officeId, CurrentOrganizationId);
            if (office == null)
                return BadRequest("Office not found");

            var fileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                CurrentOrganizationId,
                office.Name,
                path,
                ImageType.TicketImage);

            if (fileDetails == null || string.IsNullOrWhiteSpace(fileDetails.File))
                return NotFound("Ticket image not found");

            var bytes = Convert.FromBase64String(fileDetails.File);
            return File(bytes, string.IsNullOrWhiteSpace(fileDetails.ContentType) ? "image/jpeg" : fileDetails.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ticket image");
            return ServerError("An error occurred while loading the ticket image");
        }
    }

    private async Task SyncTicketImagesAsync(Guid ticketId, int officeId, string? description, IEnumerable<string?> notes)
    {
        var referencedPaths = ExtractTicketImagePaths(description);
        foreach (var note in notes)
            referencedPaths.UnionWith(ExtractTicketImagePaths(note));

        var existing = await _ticketRepository.GetTicketImagesByTicketIdAsync(ticketId);
        var existingPaths = existing
            .Select(image => image.StoragePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in referencedPaths.Where(path => !existingPaths.Contains(path)))
            await _ticketRepository.AddTicketImageAsync(ticketId, path, CurrentUser);

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, CurrentOrganizationId);
        foreach (var image in existing.Where(image => !referencedPaths.Contains(image.StoragePath)))
        {
            await _ticketRepository.DeleteTicketImageByPathAsync(ticketId, image.StoragePath);
            if (office != null && !string.IsNullOrWhiteSpace(image.StoragePath))
            {
                await _fileService.DeleteImageAsync(
                    CurrentOrganizationId,
                    office.Name,
                    image.StoragePath,
                    ImageType.TicketImage);
            }
        }
    }

    private static string PrepareTicketHtmlForSave(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html ?? string.Empty;

        return TicketImageDataSrcRegex.Replace(html, " src=\"\"");
    }

    private static HashSet<string> ExtractTicketImagePaths(string? html)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(html))
            return paths;

        foreach (Match match in TicketImagePathRegex.Matches(html))
        {
            var path = match.Groups["path"].Value.Trim();
            if (path.Contains("/ticketimage/", StringComparison.OrdinalIgnoreCase))
                paths.Add(path);
        }

        return paths;
    }
}

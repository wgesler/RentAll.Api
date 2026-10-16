using RentAll.Api.Dtos.Contacts;
using RentAll.Api.Services;

namespace RentAll.Api.Controllers;

public partial class ContactController
{
    [HttpPost("w9/extract")]
    public async Task<IActionResult> ExtractW9([FromBody] ExtractW9Dto dto)
    {
        if (dto == null)
            return BadRequest("W-9 extract data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!_documentIntelligenceService.IsEnabled)
            return BadRequest("W-9 document extraction is not configured.");

        try
        {
            var fileBytes = ReceiptDocumentExtractionHelper.GetFileBytes(dto.FileDetails!);
            var contentType = string.IsNullOrWhiteSpace(dto.FileDetails!.ContentType)
                ? "application/octet-stream"
                : dto.FileDetails.ContentType;
            var extraction = await _documentIntelligenceService.ExtractW9Async(fileBytes, contentType);
            return Ok(W9ExtractResponseDto.FromExtraction(extraction));
        }
        catch (FormatException)
        {
            return BadRequest("W-9 file content is invalid.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting W-9 for organization {OrganizationId}", dto.OrganizationId);
            return ServerError("An error occurred while reading the W-9");
        }
    }
}

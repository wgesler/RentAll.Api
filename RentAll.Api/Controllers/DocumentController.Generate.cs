using RentAll.Api.Dtos.Documents;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Controllers
{
    public partial class DocumentController
    {
        [HttpPost("generate-download")]
        public async Task<IActionResult> GeneratePdfDownload([FromBody] GenerateDocumentFromHtmlDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Document data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage ?? "Invalid request data" });

            try
            {
                // Generate PDF from HTML
                var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(dto.HtmlContent);
                var fileName = dto.FileName ?? $"document-{Guid.NewGuid()}.pdf";

                // Return as file download
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for download");
                return StatusCode(500, new { message = "An error occurred while generating the PDF" });
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateFromHtml([FromBody] GenerateDocumentFromHtmlDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Document data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage ?? "Invalid request data" });

            try
            {
                // Generate PDF from HTML
                var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(dto.HtmlContent);
                var pdfBase64 = Convert.ToBase64String(pdfBytes);

                var fileName = dto.FileName ?? $"document-{Guid.NewGuid()}.pdf";
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var pdfFileDetails = new FileDetails { File = pdfBase64, FileName = fileName, ContentType = "application/pdf" };

                // Check if document exists by name
                var existing = await _documentRepository.GetDocumentByNameAsync(fileNameWithoutExtension, CurrentOrganizationId);

                Document result;
                if (existing != null)
                {
                    // Update existing document (including deleted ones - restore them)
                    var model = new Document
                    {
                        DocumentId = existing.DocumentId,
                        OrganizationId = existing.OrganizationId,
                        OfficeId = dto.OfficeId ?? existing.OfficeId,
                        PropertyId = dto.PropertyId,
                        ReservationId = dto.ReservationId,
                        DocumentType = (DocumentType)dto.DocumentTypeId,
                        FileName = fileNameWithoutExtension,
                        FileExtension = Path.GetExtension(fileName),
                        ContentType = "application/pdf",
                        DocumentPath = string.Empty, // Will be set when saving file
                        IsDeleted = false, // Always set to not deleted
                        CreatedOn = existing.CreatedOn,
                        CreatedBy = existing.CreatedBy,
                        ModifiedBy = CurrentUser
                    };

                    model.DocumentPath = await _fileAttachmentHelper.ResolveDocumentPathForUpdateAsync(existing.OrganizationId, existing.OfficeName,
                        pdfFileDetails, (DocumentType)dto.DocumentTypeId, existing.DocumentPath, existing.DocumentPath) ?? string.Empty;
                    result = await _documentRepository.UpdateByIdAsync(model);
                }
                else
                {
                    var model = new Document
                    {
                        OrganizationId = CurrentOrganizationId,
                        OfficeId = dto.OfficeId,
                        PropertyId = dto.PropertyId,
                        ReservationId = dto.ReservationId,
                        DocumentType = (DocumentType)dto.DocumentTypeId,
                        FileName = fileNameWithoutExtension,
                        FileExtension = Path.GetExtension(fileName),
                        ContentType = "application/pdf",
                        DocumentPath = string.Empty,
                        IsDeleted = false,
                        CreatedBy = CurrentUser
                    };

                    model.DocumentPath = await _fileAttachmentHelper.SaveDocumentIfPresentAsync(CurrentOrganizationId, await GetOfficeNameAsync(dto.OfficeId),
                        pdfFileDetails, (DocumentType)dto.DocumentTypeId) ?? string.Empty;
                    result = await _documentRepository.CreateAsync(model);
                }

                var response = new DocumentResponseDto(result);
                response.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(result.OrganizationId, result.OfficeName, result.DocumentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating document from HTML");
                return StatusCode(500, new { message = "An error occurred while generating the document" });
            }
        }

    }
}


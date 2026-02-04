using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Documents;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers
{
	public partial class DocumentController
	{
		/// <summary>
		/// Generate PDF from HTML content and return as file download (does not save to server)
		/// </summary>
		/// <param name="dto">Document data with HTML content</param>
		/// <returns>PDF file download</returns>
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

		/// <summary>
		/// Generate PDF from HTML content and upsert as document (creates if doesn't exist, updates if it does)
		/// </summary>
		/// <param name="dto">Document data with HTML content</param>
		/// <returns>Created or updated document</returns>
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

				// Check if document exists by name
				var existing = await _documentRepository.GetByNameAsync(fileNameWithoutExtension, CurrentOrganizationId);

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

					// Delete old document file if it exists
					if (!string.IsNullOrWhiteSpace(existing.DocumentPath))
						await _fileService.DeleteDocumentAsync(existing.DocumentPath);

					// Save new PDF file
					var documentPath = await _fileService.SaveDocumentAsync(pdfBase64, fileName, "application/pdf", (DocumentType)dto.DocumentTypeId);
					model.DocumentPath = documentPath;
					result = await _documentRepository.UpdateByIdAsync(model);
				}
				else
				{
					// Create new document
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

					// Save PDF file
					var documentPath = await _fileService.SaveDocumentAsync(pdfBase64, fileName, "application/pdf", (DocumentType)dto.DocumentTypeId);
					model.DocumentPath = documentPath;
					result = await _documentRepository.CreateAsync(model);
				}

				var response = new DocumentResponseDto(result);
				response.FileDetails = await _fileService.GetDocumentDetailsAsync(result.DocumentPath);

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


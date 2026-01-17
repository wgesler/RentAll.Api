using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Documents;
using RentAll.Domain.Enums;

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

			if (string.IsNullOrWhiteSpace(dto.HtmlContent))
				return BadRequest(new { message = "HTML content is required" });

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
		/// Generate PDF from HTML content and save as document
		/// </summary>
		/// <param name="dto">Document data with HTML content</param>
		/// <returns>Created document</returns>
		[HttpPost("generate")]
		public async Task<IActionResult> GenerateFromHtml([FromBody] GenerateDocumentFromHtmlDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Document data is required" });

			if (string.IsNullOrWhiteSpace(dto.HtmlContent))
				return BadRequest(new { message = "HTML content is required" });

			try
			{
				// Generate PDF from HTML
				var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(dto.HtmlContent);
				var pdfBase64 = Convert.ToBase64String(pdfBytes);

				// Create document DTO
				var createDto = new CreateDocumentDto
				{
					OrganizationId = dto.OrganizationId,
					OfficeId = dto.OfficeId,
					PropertyId = dto.PropertyId,
					ReservationId = dto.ReservationId,
					DocumentTypeId = dto.DocumentTypeId,
					FileDetails = new Domain.Models.Common.FileDetails
					{
						FileName = dto.FileName ?? $"document-{Guid.NewGuid()}.pdf",
						ContentType = "application/pdf",
						File = pdfBase64
					}
				};

				var (isValid, errorMessage) = createDto.IsValid();
				if (!isValid)
					return BadRequest(new { message = errorMessage });

				var model = createDto.ToModel(CurrentOrganizationId, CurrentUser);

				// Save PDF file
				var documentPath = await _fileService.SaveDocumentAsync(
					pdfBase64,
					createDto.FileDetails!.FileName,
					"application/pdf",
					(DocumentType)dto.DocumentTypeId);
				model.DocumentPath = documentPath;

				var created = await _documentRepository.CreateAsync(model);
				var response = new DocumentResponseDto(created);
				if (!string.IsNullOrWhiteSpace(created.DocumentPath))
				{
					response.FileDetails = await _fileService.GetDocumentDetailsAsync(created.DocumentPath);
				}
				return CreatedAtAction(nameof(GetById), new { id = created.DocumentId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating document from HTML");
				return StatusCode(500, new { message = "An error occurred while generating the document" });
			}
		}
	}
}


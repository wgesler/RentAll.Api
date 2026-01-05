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

				var model = createDto.ToModel(CurrentOrganizationId, string.Empty, CurrentUser);

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

		/// <summary>
		/// Generate PDF from PropertyLetter and save as document
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <param name="officeId">Optional Office ID</param>
		/// <returns>Created document</returns>
		[HttpPost("generate/property-letter/{propertyId}")]
		public async Task<IActionResult> GeneratePropertyLetterPdf(Guid propertyId, [FromQuery] int? officeId = null)
		{
			if (propertyId == Guid.Empty)
				return BadRequest(new { message = "Property ID is required" });

			try
			{
				// Get PropertyLetter - you'll need to inject IPropertyLetterRepository
				// For now, this is a placeholder - you'll need to implement the repository call
				// var propertyLetter = await _propertyLetterRepository.GetByPropertyIdAsync(propertyId);
				// if (propertyLetter == null)
				//     return NotFound(new { message = "Property letter not found" });

				// Generate HTML from PropertyLetter
				// var htmlContent = GeneratePropertyLetterHtml(propertyLetter);

				// Generate PDF
				// var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(htmlContent);
				// ... rest of the logic

				return BadRequest(new { message = "PropertyLetter repository not yet integrated" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating property letter PDF: {PropertyId}", propertyId);
				return StatusCode(500, new { message = "An error occurred while generating the property letter PDF" });
			}
		}

		/// <summary>
		/// Generate PDF from LeaseInformation and save as document
		/// </summary>
		/// <param name="leaseInformationId">Lease Information ID</param>
		/// <param name="officeId">Optional Office ID</param>
		/// <returns>Created document</returns>
		[HttpPost("generate/lease/{leaseInformationId}")]
		public async Task<IActionResult> GenerateLeasePdf(Guid leaseInformationId, [FromQuery] int? officeId = null)
		{
			if (leaseInformationId == Guid.Empty)
				return BadRequest(new { message = "Lease Information ID is required" });

			try
			{
				// Get LeaseInformation - you'll need to inject ILeaseInformationRepository
				// For now, this is a placeholder - you'll need to implement the repository call
				// var leaseInfo = await _leaseInformationRepository.GetByIdAsync(leaseInformationId);
				// if (leaseInfo == null)
				//     return NotFound(new { message = "Lease information not found" });

				// Generate HTML from LeaseInformation
				// var htmlContent = GenerateLeaseInformationHtml(leaseInfo);

				// Generate PDF
				// var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(htmlContent);
				// ... rest of the logic

				return BadRequest(new { message = "LeaseInformation repository not yet integrated" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating lease PDF: {LeaseInformationId}", leaseInformationId);
				return StatusCode(500, new { message = "An error occurred while generating the lease PDF" });
			}
		}
	}
}


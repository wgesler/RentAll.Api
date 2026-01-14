using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Documents;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class DocumentController
	{
		/// <summary>
		/// Create a new document
		/// </summary>
		/// <param name="dto">Document data</param>
		/// <returns>Created document</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateDocumentDto dto)
		{
		if (dto == null)
			return BadRequest("Document data is required");

		var (isValid, errorMessage) = dto.IsValid();
		if (!isValid)
			return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var model = dto.ToModel(CurrentOrganizationId, CurrentUser);

				// Handle file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Save document file - we'll need to extend FileService for documents
						// For now, using a similar pattern to logos
						var documentPath = await _fileService.SaveDocumentAsync(
							dto.FileDetails.File,
							dto.FileDetails.FileName,
							dto.FileDetails.ContentType,
							(DocumentType)dto.DocumentTypeId);
						model.DocumentPath = documentPath;
					}
					catch (Exception ex)
					{
					_logger.LogError(ex, "Error saving document file");
					return ServerError("An error occurred while saving the document file");
					}
				}

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
			_logger.LogError(ex, "Error creating document");
			return ServerError("An error occurred while creating the document");
			}
		}
	}
}



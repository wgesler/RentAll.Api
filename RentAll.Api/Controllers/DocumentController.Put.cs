using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Documents;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class DocumentController
	{
		/// <summary>
		/// Update an existing document
		/// </summary>
		/// <param name="dto">Document data</param>
		/// <returns>Updated document</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UpdateDocumentDto dto)
		{
			if (dto == null)
				return BadRequest("Document data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var existing = await _documentRepository.GetByIdAsync(dto.DocumentId, CurrentOrganizationId);
				if (existing == null || existing.IsDeleted)
					return NotFound("Document not found");

				var model = dto.ToModel(CurrentUser);

				// Handle file upload if provided (replacing existing file)
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old document if it exists
						if (!string.IsNullOrWhiteSpace(existing.DocumentPath))
							await _fileService.DeleteDocumentAsync(existing.OrganizationId, existing.OfficeId, existing.DocumentPath);

						// Save new document
						var documentPath = await _fileService.SaveDocumentAsync(
							existing.OrganizationId,
							existing.OfficeId,
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

				var updated = await _documentRepository.UpdateByIdAsync(model);
				var response = new DocumentResponseDto(updated);
				if (!string.IsNullOrWhiteSpace(updated.DocumentPath))
				{
					response.FileDetails = await _fileService.GetDocumentDetailsAsync(updated.OrganizationId, updated.OfficeId, updated.DocumentPath);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating document: {DocumentId}", dto.DocumentId);
				return ServerError("An error occurred while updating the document");
			}
		}
	}
}



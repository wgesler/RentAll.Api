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
		/// <param name="id">Document ID</param>
		/// <param name="dto">Document data</param>
		/// <returns>Updated document</returns>
		[HttpPut("{id}")]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Document data is required" });

			var (isValid, errorMessage) = dto.IsValid(id);
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				var existing = await _documentRepository.GetByIdAsync(id, CurrentOrganizationId);
				if (existing == null || existing.IsDeleted)
					return NotFound(new { message = "Document not found" });

				var model = dto.ToModel(CurrentUser);

				// Handle file upload if provided (replacing existing file)
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old document if it exists
						if (!string.IsNullOrWhiteSpace(existing.DocumentPath))
							await _fileService.DeleteDocumentAsync(existing.DocumentPath);

						// Save new document
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
						return StatusCode(500, new { message = "An error occurred while saving the document file" });
					}
				}

				var updated = await _documentRepository.UpdateByIdAsync(model);
				var response = new DocumentResponseDto(updated);
				if (!string.IsNullOrWhiteSpace(updated.DocumentPath))
				{
					response.FileDetails = await _fileService.GetDocumentDetailsAsync(updated.DocumentPath);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating document: {DocumentId}", id);
				return StatusCode(500, new { message = "An error occurred while updating the document" });
			}
		}
	}
}


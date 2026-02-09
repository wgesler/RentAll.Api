using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Documents;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

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
							CurrentOrganizationId,
							dto.OfficeId,
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
					response.FileDetails = await _fileService.GetDocumentDetailsAsync(created.OrganizationId, created.OfficeId, created.DocumentPath);
				}
				return CreatedAtAction(nameof(GetById), new { id = created.DocumentId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating document");
				return ServerError("An error occurred while creating the document");
			}
		}

		/// <summary>
		/// Upsert a document by name - creates if it doesn't exist, updates if it does
		/// </summary>
		/// <param name="dto">Document data with file name</param>
		/// <returns>Created or updated document</returns>
		[HttpPost("upsert")]
		public async Task<IActionResult> UpsertByName([FromBody] UpsertDocumentDto dto)
		{
			if (dto == null)
				return BadRequest("Document data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				// Check if document exists by name
				var existing = await _documentRepository.GetByNameAsync(dto.FileName, CurrentOrganizationId);

				Document result;
				if (existing != null)
				{
					// Update existing document (including deleted ones - restore them)
					var model = dto.ToModelForUpdate(existing, CurrentUser);
					model.IsDeleted = false; // Always set to not deleted

					// Handle file upload (replacing existing file)
					if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
					{
						try
						{
							// Delete old document file if it exists
							if (!string.IsNullOrWhiteSpace(existing.DocumentPath))
								await _fileService.DeleteDocumentAsync(existing.OrganizationId, existing.OfficeId, existing.DocumentPath);

							// Save new document file
							var documentPath = await _fileService.SaveDocumentAsync(existing.OrganizationId, existing.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, 
								dto.FileDetails.ContentType, (DocumentType)dto.DocumentTypeId);
							model.DocumentPath = documentPath;
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error saving document file during upsert");
							return ServerError("An error occurred while saving the document file");
						}
					}

					result = await _documentRepository.UpdateByIdAsync(model);
				}
				else
				{
					// Create new document
					var model = dto.ToModel(CurrentOrganizationId, CurrentUser);

					// Handle file upload if provided
					if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
					{
						try
						{
							// Save document file
							var documentPath = await _fileService.SaveDocumentAsync(CurrentOrganizationId, dto.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName,
								dto.FileDetails.ContentType, (DocumentType)dto.DocumentTypeId);
							model.DocumentPath = documentPath;
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error saving document file during upsert");
							return ServerError("An error occurred while saving the document file");
						}
					}

					result = await _documentRepository.CreateAsync(model);
				}

				var response = new DocumentResponseDto(result);
				response.FileDetails = await _fileService.GetDocumentDetailsAsync(result.OrganizationId, result.OfficeId, result.DocumentPath);

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error upserting document by name: {FileName}", dto.FileName);
				return ServerError("An error occurred while upserting the document");
			}
		}
	}
}



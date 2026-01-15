using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Documents;

namespace RentAll.Api.Controllers
{
	public partial class DocumentController
	{
		/// <summary>
		/// Get all documents
		/// </summary>
		/// <returns>List of documents</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var documents = await _documentRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
				var response = new List<DocumentResponseDto>();
				foreach (var document in documents.Where(d => !d.IsDeleted))
				{
					var dto = new DocumentResponseDto(document);
					if (!string.IsNullOrWhiteSpace(document.DocumentPath))
						dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.DocumentPath);

					response.Add(dto);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error getting all documents");
			return ServerError("An error occurred while retrieving documents");
			}
		}

		/// <summary>
		/// Get document by ID
		/// </summary>
		/// <param name="id">Document ID</param>
		/// <returns>Document</returns>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(Guid id)
		{
		if (id == Guid.Empty)
			return BadRequest("Document ID is required");

			try
			{
				var document = await _documentRepository.GetByIdAsync(id, CurrentOrganizationId);
			if (document == null || document.IsDeleted)
				return NotFound("Document not found");

				var response = new DocumentResponseDto(document);
				if (!string.IsNullOrWhiteSpace(document.DocumentPath))
					response.FileDetails = await _fileService.GetDocumentDetailsAsync(document.DocumentPath);

				return Ok(response);
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error getting document by ID: {DocumentId}", id);
			return ServerError("An error occurred while retrieving the document");
			}
		}

		/// <summary>
		/// Get documents by office ID
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <returns>List of documents</returns>
		[HttpGet("office/{officeId}")]
		public async Task<IActionResult> GetByOfficeId(int officeId)
		{
		if (officeId <= 0)
			return BadRequest("Office ID is required");

			try
			{
				var documents = await _documentRepository.GetByOfficeIdAsync(officeId, CurrentOrganizationId);
				var response = new List<DocumentResponseDto>();
				foreach (var document in documents.Where(d => !d.IsDeleted))
				{
					var dto = new DocumentResponseDto(document);
					if (!string.IsNullOrWhiteSpace(document.DocumentPath))
						dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.DocumentPath);

					response.Add(dto);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error getting documents by office ID: {OfficeId}", officeId);
			return ServerError("An error occurred while retrieving documents");
			}
		}

		/// <summary>
		/// Get documents by document type
		/// </summary>
		/// <param name="documentType">Document type</param>
		/// <returns>List of documents</returns>
		[HttpGet("type/{documentType}")]
		public async Task<IActionResult> GetByDocumentType(int documentType)
		{
			try
			{
				var documents = await _documentRepository.GetByDocumentTypeAsync(documentType, CurrentOrganizationId);
				var response = new List<DocumentResponseDto>();
				foreach (var document in documents.Where(d => !d.IsDeleted))
				{
					var dto = new DocumentResponseDto(document);
					if (!string.IsNullOrWhiteSpace(document.DocumentPath))
						dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.DocumentPath);

					response.Add(dto);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error getting documents by type: {DocumentType}", documentType);
			return ServerError("An error occurred while retrieving documents");
			}
		}
	}
}



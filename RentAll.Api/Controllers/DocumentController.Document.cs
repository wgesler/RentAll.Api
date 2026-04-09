using RentAll.Api.Dtos.Documents;

namespace RentAll.Api.Controllers
{
    public partial class DocumentController
    {

        #region Get
        [HttpGet]
        public async Task<IActionResult> GetDocumentsByOfficeIdsAsync()
        {
            try
            {
                var documents = await _documentRepository.GetDocumentsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = new List<DocumentResponseDto>();
                foreach (var document in documents.Where(d => !d.IsDeleted))
                {
                    var dto = new DocumentResponseDto(document);
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

        [HttpGet("property/{propertyId:guid}/type/{id:int}")]
        public async Task<IActionResult> GetDocumentsByPropertyTypeAsync(Guid propertyId, int id)
        {
            try
            {
                var documents = await _documentRepository.GetDocumentsByPropertyTypeAsync(CurrentOrganizationId, propertyId, id, CurrentOfficeAccess);
                var response = new List<DocumentResponseDto>();
                foreach (var document in documents.Where(d => !d.IsDeleted))
                {
                    var dto = new DocumentResponseDto(document);
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

        [HttpGet("office/{officeId}")]
        public async Task<IActionResult> GetDocumentsByOfficeIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var documents = await _documentRepository.GetDocumentsByOfficeIdAsync(officeId, CurrentOrganizationId);
                var response = new List<DocumentResponseDto>();
                foreach (var document in documents.Where(d => !d.IsDeleted))
                {
                    var dto = new DocumentResponseDto(document);
                    dto.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(document.OrganizationId, document.OfficeName, document.DocumentPath);
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

        [HttpGet("type/{documentType}")]
        public async Task<IActionResult> GetDocumentsByDocumentTypeAsync(int documentType)
        {
            try
            {
                var documents = await _documentRepository.GetDocumentsByDocumentTypeAsync(documentType, CurrentOrganizationId);
                var response = new List<DocumentResponseDto>();
                foreach (var document in documents.Where(d => !d.IsDeleted))
                {
                    var dto = new DocumentResponseDto(document);
                    dto.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(document.OrganizationId, document.OfficeName, document.DocumentPath);
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

        [HttpGet("{documentId}")]
        public async Task<IActionResult> GetDocumentByIdAsync(Guid documentId)
        {
            if (documentId == Guid.Empty)
                return BadRequest("Document ID is required");

            try
            {
                var document = await _documentRepository.GetDocumentByIdAsync(documentId, CurrentOrganizationId);
                if (document == null || document.IsDeleted)
                    return NotFound("Document not found");

                var response = new DocumentResponseDto(document);
                response.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(document.OrganizationId, document.OfficeName, document.DocumentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document by ID: {DocumentId}", documentId);
                return ServerError("An error occurred while retrieving the document");
            }
        }
        #endregion

        #region Post
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

                model.DocumentPath = await _fileAttachmentHelper.SaveDocumentIfPresentAsync(CurrentOrganizationId, await GetOfficeNameAsync(dto.OfficeId), dto.FileDetails,
                    (DocumentType)dto.DocumentTypeId) ?? string.Empty;

                var created = await _documentRepository.CreateAsync(model);
                var response = new DocumentResponseDto(created);
                response.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(created.OrganizationId, created.OfficeName, created.DocumentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                return ServerError("An error occurred while creating the document");
            }
        }

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
                var existing = await _documentRepository.GetDocumentByNameAsync(dto.FileName, CurrentOrganizationId);

                Document result;
                if (existing != null)
                {
                    // Update existing document (including deleted ones - restore them)
                    var model = dto.ToModelForUpdate(existing, CurrentUser);
                    model.IsDeleted = false;
                    model.DocumentPath = await _fileAttachmentHelper.ResolveDocumentPathForUpdateAsync(existing.OrganizationId, existing.OfficeName,
                        dto.FileDetails, (DocumentType)dto.DocumentTypeId, existing.DocumentPath, null) ?? string.Empty;

                    result = await _documentRepository.UpdateByIdAsync(model);
                }
                else
                {
                    var model = dto.ToModel(CurrentOrganizationId, CurrentUser);
                    model.DocumentPath = await _fileAttachmentHelper.SaveDocumentIfPresentAsync(CurrentOrganizationId, await GetOfficeNameAsync(dto.OfficeId),
                        dto.FileDetails, (DocumentType)dto.DocumentTypeId) ?? string.Empty;

                    result = await _documentRepository.CreateAsync(model);
                }

                var response = new DocumentResponseDto(result);
                response.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(result.OrganizationId, result.OfficeName, result.DocumentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting document by name: {FileName}", dto.FileName);
                return ServerError("An error occurred while upserting the document");
            }
        }

        #endregion

        #region Put
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
                var existing = await _documentRepository.GetDocumentByIdAsync(dto.DocumentId, CurrentOrganizationId);
                if (existing == null || existing.IsDeleted)
                    return NotFound("Document not found");

                var model = dto.ToModel(CurrentUser);

                model.DocumentPath = await _fileAttachmentHelper.ResolveDocumentPathForUpdateAsync(existing.OrganizationId, existing.OfficeName,
                    dto.FileDetails, (DocumentType)dto.DocumentTypeId, existing.DocumentPath, null) ?? string.Empty;

                var updated = await _documentRepository.UpdateByIdAsync(model);
                var response = new DocumentResponseDto(updated);
                response.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(updated.OrganizationId, existing.OfficeName, updated.DocumentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document: {DocumentId}", dto.DocumentId);
                return ServerError("An error occurred while updating the document");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("{documentId}")]
        public async Task<IActionResult> DeleteDocumentByIdAsync(Guid documentId)
        {
            if (documentId == Guid.Empty)
                return BadRequest("Document ID is required");

            try
            {
                var document = await _documentRepository.GetDocumentByIdAsync(documentId, CurrentOrganizationId);
                if (document != null && document.DocumentPath != null)
                    await _fileService.DeleteDocumentAsync(document.OrganizationId, await GetOfficeNameAsync(document.OfficeId), document.DocumentPath);

                await _documentRepository.DeleteDocumentByIdAsync(documentId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
                return ServerError("An error occurred while deleting the document");
            }
        }

        #endregion

    }
}

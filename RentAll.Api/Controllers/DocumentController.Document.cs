using RentAll.Api.Dtos.Documents;

namespace RentAll.Api.Controllers
{
    public partial class DocumentController
    {

        #region Get
        [HttpPost("search")]
        public async Task<IActionResult> GetDocumentsAsync([FromBody] GetDocumentsDto dto)
        {
            if (dto == null)
                return BadRequest("Document search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            if (!UserHasOfficeAccessForAll(dto.ResolvedOfficeIds))
                return Ok();

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var documents = await _documentRepository.GetDocumentsAsync(criteria);
                var response = new List<DocumentResponseDto>();
                foreach (var document in documents.Where(d => !d.IsDeleted))
                {
                    var documentDto = new DocumentResponseDto(document);
                    response.Add(documentDto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents");
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

        private bool UserHasOfficeAccessForAll(string officeIds)
        {
            if (string.IsNullOrWhiteSpace(CurrentOfficeAccess))
                return true;

            var allowed = CurrentOfficeAccess
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToHashSet();

            return officeIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .All(id => int.TryParse(id, out var parsed) && allowed.Contains(parsed));
        }

    }
}

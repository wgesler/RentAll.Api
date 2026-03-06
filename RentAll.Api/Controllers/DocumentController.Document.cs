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
                    if (!string.IsNullOrWhiteSpace(document.DocumentPath))
                        dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.OrganizationId, document.OfficeName, document.DocumentPath);

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
                    if (!string.IsNullOrWhiteSpace(document.DocumentPath))
                        dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.OrganizationId, document.OfficeName, document.DocumentPath);

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
                if (!string.IsNullOrWhiteSpace(document.DocumentPath))
                    response.FileDetails = await _fileService.GetDocumentDetailsAsync(document.OrganizationId, document.OfficeName, document.DocumentPath);

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

                // Handle file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        // Save document file - we'll need to extend FileService for documents
                        // For now, using a similar pattern to logos
                        var documentPath = await _fileService.SaveDocumentAsync(
                            CurrentOrganizationId,
                            GetOfficeName(dto.OfficeId),
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
                    response.FileDetails = await _fileService.GetDocumentDetailsAsync(created.OrganizationId, created.OfficeName, created.DocumentPath);

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
                    model.IsDeleted = false; // Always set to not deleted

                    // Handle file upload (replacing existing file)
                    if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                    {
                        try
                        {
                            // Delete old document file if it exists
                            if (!string.IsNullOrWhiteSpace(existing.DocumentPath))
                                await _fileService.DeleteDocumentAsync(existing.OrganizationId, existing.OfficeName, existing.DocumentPath);

                            // Save new document file
                            var documentPath = await _fileService.SaveDocumentAsync(existing.OrganizationId, existing.OfficeName, dto.FileDetails.File, dto.FileDetails.FileName,
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
                            var documentPath = await _fileService.SaveDocumentAsync(CurrentOrganizationId, GetOfficeName(dto.OfficeId), dto.FileDetails.File, dto.FileDetails.FileName,
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
                response.FileDetails = await _fileService.GetDocumentDetailsAsync(result.OrganizationId, result.OfficeName, result.DocumentPath);

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

                // Handle file upload if provided (replacing existing file)
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        // Delete old document if it exists
                        if (!string.IsNullOrWhiteSpace(existing.DocumentPath))
                            await _fileService.DeleteDocumentAsync(existing.OrganizationId, existing.OfficeName, existing.DocumentPath);

                        // Save new document
                        var documentPath = await _fileService.SaveDocumentAsync(
                            existing.OrganizationId,
                            existing.OfficeName,
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
                    response.FileDetails = await _fileService.GetDocumentDetailsAsync(updated.OrganizationId, existing.OfficeName, updated.DocumentPath);

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
                    await _fileService.DeleteDocumentAsync(document.OrganizationId, GetOfficeName(document.OfficeId), document.DocumentPath);

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

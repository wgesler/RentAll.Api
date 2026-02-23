using RentAll.Api.Dtos.Documents;

namespace RentAll.Api.Controllers
{
    public partial class DocumentController
    {

        #region Get

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
        /// Get all documents by property and type
        /// </summary>
        /// <returns>List of documents by property and type</returns>
        [HttpGet("property/{propertyId:guid}/type/{id:int}")]
        public async Task<IActionResult> GetAllByPropertyAndType(Guid propertyId, int id)
        {
            try
            {
                var documents = await _documentRepository.GetAllByPropertyTypeAsync(CurrentOrganizationId, propertyId, id, CurrentOfficeAccess);
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

        /// <summary>
        /// Get document by ID
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Document</returns>
        [HttpGet("{documentId}")]
        public async Task<IActionResult> GetById(Guid documentId)
        {
            if (documentId == Guid.Empty)
                return BadRequest("Document ID is required");

            try
            {
                var document = await _documentRepository.GetByIdAsync(documentId, CurrentOrganizationId);
                if (document == null || document.IsDeleted)
                    return NotFound("Document not found");

                var response = new DocumentResponseDto(document);
                if (!string.IsNullOrWhiteSpace(document.DocumentPath))
                    response.FileDetails = await _fileService.GetDocumentDetailsAsync(document.OrganizationId, document.OfficeId, document.DocumentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document by ID: {DocumentId}", documentId);
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
                        dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.OrganizationId, document.OfficeId, document.DocumentPath);

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
                        dto.FileDetails = await _fileService.GetDocumentDetailsAsync(document.OrganizationId, document.OfficeId, document.DocumentPath);

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

        #endregion

        #region Post

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
                return CreatedAtAction(nameof(GetById), new { documentId = created.DocumentId }, response);
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

        #endregion

        #region Put

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

        #endregion

        #region Delete

        /// <summary>
        /// Delete a document (soft delete)
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{documentId}")]
        public async Task<IActionResult> Delete(Guid documentId)
        {
            if (documentId == Guid.Empty)
                return BadRequest("Document ID is required");

            try
            {
                var existing = await _documentRepository.GetByIdAsync(documentId, CurrentOrganizationId);
                if (existing == null || existing.IsDeleted)
                    return NotFound("Document not found");

                await _documentRepository.DeleteByIdAsync(documentId, CurrentOrganizationId);
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

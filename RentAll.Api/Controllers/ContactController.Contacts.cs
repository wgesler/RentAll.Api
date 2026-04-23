using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        #region Get
        [HttpGet]
        public async Task<IActionResult> GetContactsByOfficeIdAsync()
        {
            try
            {
                var contacts = await _contactRepository.GetContactsByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = contacts.Select(c => new ContactResponseDto(c));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contacts");
                return ServerError("An error occurred while retrieving contacts");
            }
        }

        [HttpGet("type/{contactTypeId}")]
        public async Task<IActionResult> GetContactsByContactTypeIdAsync(int contactTypeId)
        {
            try
            {
                var contacts = await _contactRepository.GetContactsByContactTypeIdAsync(contactTypeId, CurrentOrganizationId);
                var response = contacts.Select(c => new ContactResponseDto(c));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts by ContactTypeId: {ContactTypeId}", contactTypeId);
                return ServerError("An error occurred while retrieving contacts");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Contact ID is required");

            try
            {
                var contact = await _contactRepository.GetContactByIdAsync(id, CurrentOrganizationId);
                if (contact == null)
                    return NotFound("Contact not found");

                // Get the office name for file storage path
                var office = await _organizationRepository.GetOfficeByIdAsync(contact.OfficeId, contact.OrganizationId);
                var officeName = office != null ? office.Name : null;

                // Get W9 and Insurance file details if paths are available
                var response = new ContactResponseDto(contact);
                response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(contact.OrganizationId, officeName, contact.W9Path, ImageType.W9Forms);
                response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(contact.OrganizationId, officeName, contact.InsurancePath, ImageType.Insurances);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact by ID: {ContactId}", id);
                return ServerError("An error occurred while retrieving the contact");
            }
        }
        #endregion

        #region Post
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContactDto dto)
        {
            if (dto == null)
                return BadRequest("Contact data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid || !IsValidEmail(dto.Email))
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Get a new Contact code
                var code = await _contactManager.GenerateContactCodeAsync(dto.OrganizationId, dto.EntityTypeId);
                var contact = dto.ToModel(code, CurrentUser);

                // Get the office name for file storage path
                var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, dto.OrganizationId);
                var officeName = office != null ? office.Name : null;

                contact.W9Path = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, officeName, dto.W9FileDetails, ImageType.W9Forms);
                contact.InsurancePath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, officeName, dto.InsuranceFileDetails, ImageType.Insurances);

                var createdContact = await _contactRepository.CreateAsync(contact);
                var afterLogin = await _contactManager.GenerateLoginForOwnerContact(createdContact, CurrentUser);
                var response = new ContactResponseDto(afterLogin);

                response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(afterLogin.OrganizationId, null, afterLogin.W9Path, ImageType.W9Forms);
                response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(afterLogin.OrganizationId, null, afterLogin.InsurancePath, ImageType.Insurances);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return ServerError("An error occurred while creating the contact");
            }
        }
        #endregion

        #region Put
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateContactDto dto)
        {
            if (dto == null)
                return BadRequest("Contact data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid || !IsValidEmail(dto.Email))
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if contact exists
                var existing = await _contactRepository.GetContactByIdAsync(dto.ContactId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Contact not found");

                // Check if ContactCode is being changed
                if (existing.ContactCode != dto.ContactCode)
                    return BadRequest("Contact Code cannot change");

                var contact = dto.ToModel(CurrentUser);
                contact.UserId = dto.UserId ?? existing.UserId;

                // Get the office name for file storage path
                var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, dto.OrganizationId);
                var officeName = office != null ? office.Name : null;

                contact.W9Path = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(existing.OrganizationId, officeName, dto.W9FileDetails, ImageType.W9Forms, existing.W9Path, dto.W9Path);
                contact.InsurancePath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(existing.OrganizationId, officeName, dto.InsuranceFileDetails, ImageType.Insurances, existing.InsurancePath, dto.InsurancePath);

                var updatedContact = await _contactRepository.UpdateByIdAsync(contact);
                var response = new ContactResponseDto(updatedContact);

                response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updatedContact.OrganizationId, null, updatedContact.W9Path, ImageType.W9Forms);
                response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updatedContact.OrganizationId, null, updatedContact.InsurancePath, ImageType.Insurances);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact: {ContactId}", dto.ContactId);
                return ServerError("An error occurred while updating the contact");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("{contactId}")]
        public async Task<IActionResult> DeleteContactByIdAsync(Guid contactId)
        {
            if (contactId == Guid.Empty)
                return BadRequest("Contact ID is required");

            try
            {
                // Check if contact exists then check/delete w9 and insurance files
                var existing = await _contactRepository.GetContactByIdAsync(contactId, CurrentOrganizationId);
                if (existing != null)
                {
                    // Get the office name for file storage path
                    var office = await _organizationRepository.GetOfficeByIdAsync(existing.OfficeId, existing.OrganizationId);
                    var officeName = office != null ? office.Name : null;

                    if (!string.IsNullOrWhiteSpace(existing.W9Path))
                        await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.W9Path, ImageType.W9Forms);
                    if (!string.IsNullOrEmpty(existing.InsurancePath))
                        await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.InsurancePath, ImageType.Insurances);
                }

                await _contactRepository.DeleteContactByIdAsync(contactId, CurrentUser);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact: {ContactId}", contactId);
                return ServerError("An error occurred while deleting the contact");
            }
        }
        #endregion
    }
}

using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
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
    }
}









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
                var updatedContact = await _contactRepository.UpdateByIdAsync(contact);
                var response = new ContactResponseDto(updatedContact);

                // Get the office name for file storage path
                var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, dto.OrganizationId);
                var officeName = office != null ? office.Name : null;

                // Handle W9 details file
                if (dto.W9FileDetails != null && !string.IsNullOrWhiteSpace(dto.W9FileDetails.File))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(existing.W9Path))
                            await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.W9Path, ImageType.Profiles);

                        var w9Path = await _fileService.SaveImageAsync(existing.OrganizationId, officeName, dto.W9FileDetails.File, dto.W9FileDetails.FileName, dto.W9FileDetails.ContentType, ImageType.W9Forms);
                        contact.W9Path = w9Path;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving contact W9 form");
                        return ServerError("An error occurred while saving the W9 form");
                    }
                }
                else if (dto.W9Path == null)
                {
                    if (!string.IsNullOrWhiteSpace(existing.W9Path))
                    {
                        await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.W9Path, ImageType.W9Forms);
                        contact.W9Path = null;
                    }
                }
                else
                {
                    contact.W9Path = existing.W9Path;
                }

                // Handle Insurance details file
                if (dto.InsuranceFileDetails != null && !string.IsNullOrWhiteSpace(dto.InsuranceFileDetails.File))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(existing.InsurancePath))
                            await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.InsurancePath, ImageType.Insurances);

                        var insurancePath = await _fileService.SaveImageAsync(existing.OrganizationId, officeName, dto.InsuranceFileDetails.File, dto.InsuranceFileDetails.FileName, dto.InsuranceFileDetails.ContentType, ImageType.Insurances);
                        contact.InsurancePath = insurancePath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving contact insurance form");
                        return ServerError("An error occurred while saving the insurance form");
                    }
                }
                else if (dto.InsurancePath == null)
                {
                    if (!string.IsNullOrWhiteSpace(existing.InsurancePath))
                    {
                        await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.InsurancePath, ImageType.Insurances);
                        contact.InsurancePath = null;
                    }
                }
                else
                {
                    contact.InsurancePath = existing.InsurancePath;
                }
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









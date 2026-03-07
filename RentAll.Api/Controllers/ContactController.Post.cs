using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
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

                // Save W9 file if provided
                if (dto.W9FileDetails != null && !string.IsNullOrWhiteSpace(dto.W9FileDetails.File))
                {
                    try
                    {
                        var w9Path = await _fileService.SaveImageAsync(dto.OrganizationId, officeName, dto.W9FileDetails.File, dto.W9FileDetails.FileName, dto.W9FileDetails.ContentType, ImageType.W9Forms);
                        contact.W9Path = w9Path;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving contact w9");
                        return ServerError("An error occurred while saving the contact's w9 file");
                    }
                }

                // Save Insurance file if provided
                if (dto.InsuranceFileDetails != null && !string.IsNullOrWhiteSpace(dto.InsuranceFileDetails.File))
                {
                    try
                    {
                        var insurancePath = await _fileService.SaveImageAsync(dto.OrganizationId, officeName, dto.InsuranceFileDetails.File, dto.InsuranceFileDetails.FileName, dto.InsuranceFileDetails.ContentType, ImageType.Insurances);
                        contact.InsurancePath = insurancePath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving contact's insurance");
                        return ServerError("An error occurred while saving the contact's insurance file");
                    }
                }


                var createdContact = await _contactRepository.CreateAsync(contact);
                await _contactManager.GenerateLoginForOwnerContact(createdContact, CurrentUser);
                var response = new ContactResponseDto(createdContact);

                if (!string.IsNullOrWhiteSpace(createdContact.W9Path))
                    response.W9FileDetails = await _fileService.GetImageDetailsAsync(createdContact.OrganizationId, null, createdContact.W9Path, ImageType.W9Forms);
                if (!string.IsNullOrWhiteSpace(createdContact.InsurancePath))
                    response.InsuranceFileDetails = await _fileService.GetImageDetailsAsync(createdContact.OrganizationId, null, createdContact.InsurancePath, ImageType.Insurances);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return ServerError("An error occurred while creating the contact");
            }
        }
    }
}






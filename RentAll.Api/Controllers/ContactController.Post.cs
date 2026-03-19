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

                contact.W9Path = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, officeName, dto.W9FileDetails, ImageType.W9Forms);
                contact.InsurancePath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, officeName, dto.InsuranceFileDetails, ImageType.Insurances);
                contact.AgreementPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, officeName, dto.AgreementFileDetails, ImageType.Agreements);

                var createdContact = await _contactRepository.CreateAsync(contact);
                await _contactManager.GenerateLoginForOwnerContact(createdContact, CurrentUser);
                var response = new ContactResponseDto(createdContact);

                response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(createdContact.OrganizationId, null, createdContact.W9Path, ImageType.W9Forms);
                response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(createdContact.OrganizationId, null, createdContact.InsurancePath, ImageType.Insurances);
                response.AgreementFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(createdContact.OrganizationId, null, createdContact.AgreementPath, ImageType.Agreements);

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






using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
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
                if (!string.IsNullOrWhiteSpace(contact.W9Path))
                    response.W9FileDetails = await _fileService.GetImageDetailsAsync(contact.OrganizationId, officeName, contact.W9Path, ImageType.W9Forms);
                if (!string.IsNullOrWhiteSpace(contact.InsurancePath))
                    response.InsuranceFileDetails = await _fileService.GetImageDetailsAsync(contact.OrganizationId, officeName, contact.InsurancePath, ImageType.Insurances);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact by ID: {ContactId}", id);
                return ServerError("An error occurred while retrieving the contact");
            }
        }
    }
}










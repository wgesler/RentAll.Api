using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        /// <summary>
        /// Get all contacts
        /// </summary>
        /// <returns>List of contacts</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var contacts = await _contactRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = contacts.Select(c => new ContactResponseDto(c));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contacts");
                return ServerError("An error occurred while retrieving contacts");
            }
        }

        /// <summary>
        /// Get contact by ID
        /// </summary>
        /// <param name="id">Contact ID</param>
        /// <returns>Contact</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Contact ID is required");

            try
            {
                var contact = await _contactRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (contact == null)
                    return NotFound("Contact not found");

                return Ok(new ContactResponseDto(contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact by ID: {ContactId}", id);
                return ServerError("An error occurred while retrieving the contact");
            }
        }

        /// <summary>
        /// Get contacts by ContactTypeId
        /// </summary>
        /// <param name="contactTypeId">Contact Type ID</param>
        /// <returns>List of contacts</returns>
        [HttpGet("type/{contactTypeId}")]
        public async Task<IActionResult> GetByContactTypeId(int contactTypeId)
        {
            try
            {
                var contacts = await _contactRepository.GetByContactTypeIdAsync(contactTypeId, CurrentOrganizationId);
                var response = contacts.Select(c => new ContactResponseDto(c));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts by ContactTypeId: {ContactTypeId}", contactTypeId);
                return ServerError("An error occurred while retrieving contacts");
            }
        }
    }
}










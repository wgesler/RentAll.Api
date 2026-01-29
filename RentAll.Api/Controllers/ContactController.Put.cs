using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        /// <summary>
        /// Update an existing contact
        /// </summary>
        /// <param name="dto">Contact data</param>
        /// <returns>Updated contact</returns>
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
				var existing = await _contactRepository.GetByIdAsync(dto.ContactId, CurrentOrganizationId);
				if (existing == null)
					return NotFound("Contact not found");

				// Check if CompanyCode is being changed
				if (existing.ContactCode != dto.ContactCode)
					return BadRequest("Contact Code cannot change");


                var contact = dto.ToModel(CurrentUser);
				var updatedContact = await _contactRepository.UpdateByIdAsync(contact);
                return Ok(new ContactResponseDto(updatedContact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact: {ContactId}", dto.ContactId);
                return ServerError("An error occurred while updating the contact");
            }
        }
    }
}









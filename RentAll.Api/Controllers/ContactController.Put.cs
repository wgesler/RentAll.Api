using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        /// <summary>
        /// Update an existing contact
        /// </summary>
        /// <param name="id">Contact ID</param>
        /// <param name="dto">Contact data</param>
        /// <returns>Updated contact</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Contact data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid || !IsValidEmail(dto.Email))
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if contact exists
                var existingContact = await _contactRepository.GetByIdAsync(id);
                if (existingContact == null)
                    return NotFound(new { message = "Contact not found" });

                // Check if ContactCode is being changed and if the new code already exists
                if (existingContact.ContactCode != dto.ContactCode)
                {
                    if (await _contactRepository.ExistsByContactCodeAsync(dto.ContactCode))
                        return Conflict(new { message = "Contact Code already exists" });
                }

                var contact = dto.ToModel(dto, CurrentUser);
                var updatedContact = await _contactRepository.UpdateByIdAsync(contact);
                return Ok(new ContactResponseDto(updatedContact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact: {ContactId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the contact" });
            }
        }
    }
}




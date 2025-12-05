using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Contacts;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        /// <summary>
        /// Create a new contact
        /// </summary>
        /// <param name="dto">Contact data</param>
        /// <returns>Created contact</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContactDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Contact data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if ContactCode already exists
                if (await _contactRepository.ExistsByContactCodeAsync(dto.ContactCode))
                    return Conflict(new { message = "Contact Code already exists" });

                var contact = dto.ToModel();
                var createdContact = await _contactRepository.CreateAsync(contact);
                return CreatedAtAction(nameof(GetById), new { id = createdContact.ContactId }, new ContactResponseDto(createdContact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return StatusCode(500, new { message = "An error occurred while creating the contact" });
            }
        }
    }
}
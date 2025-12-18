using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Contacts;
using RentAll.Domain.Enums;

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
            if (!isValid || !IsValidEmail(dto.Email))
                return BadRequest(new { message = errorMessage });

            try
            {
                // Get a new Contact code
                var code = await _contactManager.GenerateContactCodeAsync(dto.OrganizationId, dto.EntityTypeId);
                var contact = dto.ToModel( code, CurrentUser);

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






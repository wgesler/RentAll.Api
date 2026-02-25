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
                return BadRequest("Contact data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid || !IsValidEmail(dto.Email))
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Get a new Contact code
                var code = await _contactManager.GenerateContactCodeAsync(dto.OrganizationId, dto.EntityTypeId);
                var contact = dto.ToModel(code, CurrentUser);

                var createdContact = await _contactRepository.CreateAsync(contact);
                await _contactManager.GenerateLoginForOwnerContact(createdContact, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = createdContact.ContactId }, new ContactResponseDto(createdContact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return ServerError("An error occurred while creating the contact");
            }
        }
    }
}






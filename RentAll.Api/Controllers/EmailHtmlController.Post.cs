using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.EmailHtmls;

namespace RentAll.Api.Controllers
{
    public partial class EmailHtmlController
    {
        /// <summary>
        /// Create email html for organization.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmailHtmlDto dto)
        {
            if (dto == null)
                return BadRequest("EmailHtml data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var emailHtml = dto.ToModel(CurrentUser);
                var created = await _emailRepository.CreateEmailHtmlAsync(emailHtml);
                return CreatedAtAction(nameof(GetByOrganization), new EmailHtmlResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while creating EmailHtml");
            }
        }
    }
}

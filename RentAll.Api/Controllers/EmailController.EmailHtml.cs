
namespace RentAll.Api.Controllers
{
    public partial class EmailController
    {

        #region Get

        /// <summary>
        /// Get email html by organization.
        /// </summary>
        [HttpGet("email-html")]
        public async Task<IActionResult> GetByOrganization()
        {
            try
            {
                var emailHtml = await _emailRepository.GetEmailHtmlByOrganizationIdAsync(CurrentOrganizationId);
                if (emailHtml == null)
                    return NotFound("EmailHtml not found");

                return Ok(new EmailHtmlResponseDto(emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while retrieving EmailHtml");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create email html for organization.
        /// </summary>
        [HttpPost("email-html")]
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

                var response = new EmailHtmlResponseDto(created);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while creating EmailHtml");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update email html for organization.
        /// </summary>
        [HttpPut("email-html")]
        public async Task<IActionResult> Update([FromBody] UpdateEmailHtmlDto dto)
        {
            if (dto == null)
                return BadRequest("EmailHtml data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _emailRepository.GetEmailHtmlByOrganizationIdAsync(CurrentOrganizationId);
                if (existing == null)
                    return NotFound("EmailHtml not found");

                var emailHtml = dto.ToModel(CurrentUser);
                var updated = await _emailRepository.UpdateEmailHtmlByOrganizationIdAsync(emailHtml);
                return Ok(new EmailHtmlResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while updating EmailHtml");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete email html for organization.
        /// </summary>
        [HttpDelete("email-html")]
        public async Task<IActionResult> Delete()
        {
            try
            {
                var existing = await _emailRepository.GetEmailHtmlByOrganizationIdAsync(CurrentOrganizationId);
                if (existing == null)
                    return NotFound("EmailHtml not found");

                await _emailRepository.DeleteEmailHtmlByOrganizationIdAsync(CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while deleting EmailHtml");
            }
        }

        #endregion

    }
}

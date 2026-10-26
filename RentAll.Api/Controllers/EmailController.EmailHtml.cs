namespace RentAll.Api.Controllers
{
    public partial class EmailController
    {

        #region Get

        [HttpGet("email-html")]
        public async Task<IActionResult> GetByOrganization([FromQuery] int? officeId, [FromQuery] Guid? organizationId)
        {
            try
            {
                var emailHtml = await _emailRepository.GetEmailHtmlByOrganizationIdAsync(
                    ResolveEmailHtmlOrganization(organizationId),
                    NormalizeOfficeId(officeId));
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

        [HttpPost("email-html")]
        public async Task<IActionResult> Create([FromBody] CreateEmailHtmlDto dto)
        {
            if (dto == null)
                return BadRequest("EmailHtml data is required");

            ApplyEmailHtmlScope(dto.OrganizationId, dto.OfficeId, out var organizationId, out var officeId);
            if (organizationId == SystemOrganizationId)
                return BadRequest("Email templates cannot be saved for the system organization.");

            dto.OrganizationId = organizationId;
            dto.OfficeId = officeId;

            var (isValid, errorMessage) = dto.IsValid(organizationId);
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
                _logger.LogError(ex, "Error creating EmailHtml for organization: {OrganizationId}", organizationId);
                return ServerError("An error occurred while creating EmailHtml");
            }
        }

        #endregion

        #region Put

        [HttpPut("email-html")]
        public async Task<IActionResult> Update([FromBody] UpdateEmailHtmlDto dto)
        {
            if (dto == null)
                return BadRequest("EmailHtml data is required");

            ApplyEmailHtmlScope(dto.OrganizationId, dto.OfficeId, out var organizationId, out var officeId);
            if (organizationId == SystemOrganizationId)
                return BadRequest("Email templates cannot be saved for the system organization.");

            dto.OrganizationId = organizationId;
            dto.OfficeId = officeId;

            var (isValid, errorMessage) = dto.IsValid(organizationId);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var emailHtml = dto.ToModel(CurrentUser);
                emailHtml.CreatedBy = CurrentUser;
                var existing = await _emailRepository.GetEmailHtmlByOrganizationIdAsync(organizationId, officeId, exactOnly: true);
                var saved = existing == null
                    ? await _emailRepository.CreateEmailHtmlAsync(emailHtml)
                    : await _emailRepository.UpdateEmailHtmlByOrganizationIdAsync(emailHtml);
                return Ok(new EmailHtmlResponseDto(saved));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating EmailHtml for organization: {OrganizationId}", organizationId);
                return ServerError("An error occurred while updating EmailHtml");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("email-html")]
        public async Task<IActionResult> DeleteEmailHtmlByOrganizationIdAsync([FromQuery] int? officeId, [FromQuery] Guid? organizationId)
        {
            try
            {
                var deleteOrganizationId = ResolveEmailHtmlOrganization(organizationId);
                if (deleteOrganizationId == SystemOrganizationId)
                    return BadRequest("Email templates cannot be saved for the system organization.");

                await _emailRepository.DeleteEmailHtmlByOrganizationIdAsync(
                    deleteOrganizationId,
                    NormalizeOfficeId(officeId));
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while deleting EmailHtml");
            }
        }

        #endregion

        private Guid ResolveEmailHtmlOrganization(Guid? organizationId)
        {
            if (IsSuperAdmin() && organizationId.HasValue && organizationId.Value != Guid.Empty)
                return organizationId.Value;

            return CurrentOrganizationId;
        }

        private void ApplyEmailHtmlScope(Guid requestedOrganizationId, int? requestedOfficeId, out Guid organizationId, out int? officeId)
        {
            organizationId = ResolveEmailHtmlOrganization(requestedOrganizationId == Guid.Empty ? null : requestedOrganizationId);
            officeId = NormalizeOfficeId(requestedOfficeId);
        }

        private static int? NormalizeOfficeId(int? officeId)
        {
            return officeId is > 0 ? officeId : null;
        }

    }
}

using RentAll.Api.Dtos.Emails.EmailTemplates;

namespace RentAll.Api.Controllers
{
    /// <summary>SendGrid template IDs per organization (and optionally office), set in Settings > Email.</summary>
    public partial class EmailController
    {
        [HttpGet("email-template/names")]
        public IActionResult GetEmailTemplateNames()
        {
            return Ok(EmailTemplateNames.All.Select(pair => new EmailTemplateNameDto { EmailName = pair.Key, DisplayName = pair.Value }));
        }

        [HttpGet("email-template")]
        public async Task<IActionResult> GetEmailTemplates([FromQuery] Guid? organizationId)
        {
            var scopeOrganizationId = ResolveEmailHtmlOrganization(organizationId);
            try
            {
                var templates = await _emailRepository.GetEmailTemplatesByOrganizationIdAsync(scopeOrganizationId);
                return Ok(templates.Select(template => new EmailTemplateResponseDto(template)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email templates for organization: {OrganizationId}", scopeOrganizationId);
                return ServerError("An error occurred while retrieving email templates");
            }
        }

        [HttpGet("email-template/{emailTemplateId:int}")]
        public async Task<IActionResult> GetEmailTemplateById(int emailTemplateId, [FromQuery] Guid? organizationId)
        {
            var scopeOrganizationId = ResolveEmailHtmlOrganization(organizationId);
            try
            {
                var template = await _emailRepository.GetEmailTemplateByIdAsync(emailTemplateId, scopeOrganizationId);
                return template == null ? NotFound("Email template not found") : Ok(new EmailTemplateResponseDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email template {EmailTemplateId}", emailTemplateId);
                return ServerError("An error occurred while retrieving the email template");
            }
        }

        /// <summary>Saves the template for an organization/office/email: updates the row if it exists, otherwise adds it.</summary>
        [HttpPut("email-template")]
        public async Task<IActionResult> SaveEmailTemplate([FromBody] SaveEmailTemplateDto dto)
        {
            if (dto == null)
                return BadRequest("Email template data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid email template");

            ApplyEmailHtmlScope(dto.OrganizationId, dto.OfficeId, out var organizationId, out var officeId);
            if (organizationId == SystemOrganizationId)
                return BadRequest("Email templates cannot be saved for the system organization.");

            try
            {
                var emailName = EmailTemplateNames.Canonical(dto.EmailName);
                var existing = (await _emailRepository.GetEmailTemplatesByOrganizationIdAsync(organizationId))
                    .FirstOrDefault(template => template.OfficeId == officeId
                        && template.EmailName.Equals(emailName, StringComparison.OrdinalIgnoreCase));

                var model = new EmailTemplate
                {
                    EmailTemplateId = existing?.EmailTemplateId ?? 0,
                    OrganizationId = organizationId,
                    OfficeId = officeId,
                    EmailName = emailName,
                    TemplateId = dto.TemplateId.Trim()
                };
                var saved = existing == null
                    ? await _emailRepository.CreateEmailTemplateAsync(model)
                    : await _emailRepository.UpdateEmailTemplateByIdAsync(model);
                return Ok(new EmailTemplateResponseDto(saved));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email template {EmailName} for organization: {OrganizationId}", dto.EmailName, organizationId);
                return ServerError("An error occurred while saving the email template");
            }
        }

        [HttpDelete("email-template/{emailTemplateId:int}")]
        public async Task<IActionResult> DeleteEmailTemplate(int emailTemplateId, [FromQuery] Guid? organizationId)
        {
            var scopeOrganizationId = ResolveEmailHtmlOrganization(organizationId);
            if (scopeOrganizationId == SystemOrganizationId)
                return BadRequest("Email templates cannot be changed for the system organization.");

            try
            {
                var existing = await _emailRepository.GetEmailTemplateByIdAsync(emailTemplateId, scopeOrganizationId);
                if (existing == null)
                    return NotFound("Email template not found");

                await _emailRepository.DeleteEmailTemplateByIdAsync(emailTemplateId, scopeOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email template {EmailTemplateId}", emailTemplateId);
                return ServerError("An error occurred while deleting the email template");
            }
        }
    }
}

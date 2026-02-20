using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Emails;

namespace RentAll.Api.Controllers
{
    public partial class EmailController
    {
        /// <summary>
        /// Get all emails.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var emails = await _emailRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = new List<EmailResponseDto>();
                foreach (var email in emails)
                {
                    var dto = new EmailResponseDto(email);
                    if (!string.IsNullOrWhiteSpace(email.AttachmentPath))
                        dto.FileDetails = await _fileService.GetDocumentDetailsAsync(email.OrganizationId, email.OfficeId, email.AttachmentPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all emails");
                return ServerError("An error occurred while retrieving emails");
            }
        }

        /// <summary>
        /// Get email by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Email ID is required");

            try
            {
                var email = await _emailRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (email == null)
                    return NotFound("Email not found");

                var response = new EmailResponseDto(email);
                if (!string.IsNullOrWhiteSpace(email.AttachmentPath))
                    response.FileDetails = await _fileService.GetDocumentDetailsAsync(email.OrganizationId, email.OfficeId, email.AttachmentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email by ID: {EmailId}", id);
                return ServerError("An error occurred while retrieving the email");
            }
        }
    }
}

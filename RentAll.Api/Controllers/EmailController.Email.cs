
namespace RentAll.Api.Controllers
{
    public partial class EmailController
    {
        #region Get

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
        [HttpGet("{emailId}")]
        public async Task<IActionResult> GetById(Guid emailId)
        {
            if (emailId == Guid.Empty)
                return BadRequest("Email ID is required");

            try
            {
                var email = await _emailRepository.GetByIdAsync(emailId, CurrentOrganizationId);
                if (email == null)
                    return NotFound("Email not found");

                var response = new EmailResponseDto(email);
                if (!string.IsNullOrWhiteSpace(email.AttachmentPath))
                    response.FileDetails = await _fileService.GetDocumentDetailsAsync(email.OrganizationId, email.OfficeId, email.AttachmentPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email by ID: {EmailId}", emailId);
                return ServerError("An error occurred while retrieving the email");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new email.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmailDto dto)
        {
            if (dto == null)
                return BadRequest("Email data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId, CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var org = await _organizationRepository.GetByIdAsync(dto.OrganizationId);
                var email = dto.ToModel(CurrentUser);
                var result = await _emailManager.SendEmail(org?.SendGridName, email);

                if (result.EmailStatus == EmailStatus.Succeeded)
                    return Ok(new EmailResponseDto(result));

                if (result.EmailStatus == EmailStatus.Failed)
                    return StatusCode(StatusCodes.Status502BadGateway, new EmailResponseDto(result));

                return Accepted(new EmailResponseDto(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email");
                return ServerError("An error occurred while creating the email");
            }
        }

        #endregion

    }
}

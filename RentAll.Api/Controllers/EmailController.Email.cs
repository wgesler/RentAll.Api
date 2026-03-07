
namespace RentAll.Api.Controllers
{
    public partial class EmailController
    {
        #region Get

        [HttpGet]
        public async Task<IActionResult> GetEmailsByOfficeIdsAsync()
        {
            try
            {
                var emails = await _emailRepository.GetEmailsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
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

        [HttpGet("{emailId}")]
        public async Task<IActionResult> GetEmailByIdAsync(Guid emailId)
        {
            if (emailId == Guid.Empty)
                return BadRequest("Email ID is required");

            try
            {
                var email = await _emailRepository.GetEmailByIdAsync(emailId, CurrentOrganizationId);
                if (email == null)
                    return NotFound("Email not found");

                var response = new EmailResponseDto(email);
                response.FileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(email.OrganizationId, email.OfficeName, email.AttachmentPath);

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
                var org = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
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

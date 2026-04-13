
namespace RentAll.Api.Controllers
{
    public partial class EmailController
    {
        #region Get

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlertsByOfficeIdsAsync()
        {
            try
            {
                var alerts = await _emailRepository.GetAlertsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = alerts.Select(a => new AlertResponseDto(a)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts");
                return ServerError("An error occurred while retrieving alerts");
            }
        }

        [HttpGet("alerts/{alertId:guid}")]
        public async Task<IActionResult> GetAlertByIdAsync(Guid alertId)
        {
            if (alertId == Guid.Empty)
                return BadRequest("Alert ID is required");

            try
            {
                var alert = await _emailRepository.GetAlertByIdAsync(alertId, CurrentOrganizationId);
                if (alert == null)
                    return NotFound("Alert not found");

                return Ok(new AlertResponseDto(alert));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert by ID: {AlertId}", alertId);
                return ServerError("An error occurred while retrieving the alert");
            }
        }

        #endregion

        #region Post

        [HttpPost("alerts")]
        public async Task<IActionResult> Create([FromBody] CreateAlertDto dto)
        {
            if (dto == null)
                return BadRequest("Alert data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId, CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var alert = dto.ToModel(CurrentUser);
                var created = await _emailRepository.CreateAlertAsync(alert);
                return Ok(new AlertResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return ServerError("An error occurred while creating the alert");
            }
        }

        #endregion

        #region Put

        [HttpPut("alerts/{alertId:guid}")]
        public async Task<IActionResult> Update(Guid alertId, [FromBody] UpdateAlertDto dto)
        {
            if (dto == null)
                return BadRequest("Alert data is required");

            if (alertId == Guid.Empty)
                return BadRequest("Alert ID is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId, CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _emailRepository.GetAlertByIdAsync(alertId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Alert not found");

                dto.ApplyTo(existing, CurrentUser);
                var updated = await _emailRepository.UpdateAlertByIdAsync(existing);
                return Ok(new AlertResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alert: {AlertId}", alertId);
                return ServerError("An error occurred while updating the alert");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("alerts/{alertId:guid}")]
        public async Task<IActionResult> DeleteAlertByIdAsync(Guid alertId)
        {
            if (alertId == Guid.Empty)
                return BadRequest("Alert ID is required");

            try
            {
                var existing = await _emailRepository.GetAlertByIdAsync(alertId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Alert not found");

                await _emailRepository.DeleteAlertByIdAsync(alertId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert: {AlertId}", alertId);
                return ServerError("An error occurred while deleting the alert");
            }
        }

        #endregion
    }
}

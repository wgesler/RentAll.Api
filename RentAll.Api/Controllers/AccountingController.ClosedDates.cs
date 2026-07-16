using RentAll.Api.Dtos.Accounting.ClosedDates;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get

        [HttpPost("closed-date/search")]
        public async Task<IActionResult> SearchClosedDates([FromBody] GetClosedDatesByCriteriaDto dto)
        {
            if (dto == null)
                return BadRequest("Closed date search criteria is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid closed date search criteria");

            try
            {
                var closedDates = await _accountingRepository.GetClosedDatesByCriteriaAsync(
                    CurrentOrganizationId,
                    dto.ToOfficeIdsCsv(),
                    dto.StartDate,
                    dto.EndDate,
                    dto.PostingStatusId);
                var response = closedDates.Select(item => new ClosedDateResponseDto(item)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching closed dates");
                return ServerError("An error occurred while retrieving closed dates");
            }
        }

        [HttpGet("closed-date/office/{officeId:int}/closedDateId/{closedDateId:int}")]
        public async Task<IActionResult> GetClosedDateById(int officeId, int closedDateId)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's closed dates");

            if (closedDateId <= 0)
                return BadRequest("Invalid closed date ID");

            try
            {
                var closedDate = await _accountingRepository.GetClosedDateByIdAsync(closedDateId, CurrentOrganizationId, officeId);
                if (closedDate == null)
                    return NotFound("Closed date not found");

                return Ok(new ClosedDateResponseDto(closedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting closed date by ID: {ClosedDateId}", closedDateId);
                return ServerError("An error occurred while retrieving the closed date");
            }
        }

        #endregion

        #region Post

        [HttpPost("closed-date")]
        public async Task<IActionResult> CreateClosedDate([FromBody] CreateClosedDateDto dto)
        {
            if (dto == null)
                return BadRequest("Closed date data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid closed date request");

            try
            {
                var closedDate = dto.ToModel(CurrentOrganizationId);
                var createdClosedDate = await _accountingRepository.CreateClosedDateAsync(closedDate);
                return Ok(new ClosedDateResponseDto(createdClosedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating closed date");
                return ServerError("An error occurred while creating the closed date");
            }
        }

        #endregion

        #region Put

        [HttpPut("closed-date")]
        public async Task<IActionResult> UpdateClosedDate([FromBody] UpdateClosedDateDto dto)
        {
            if (dto == null)
                return BadRequest("Closed date data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid closed date request");

            try
            {
                var existingClosedDate = await _accountingRepository.GetClosedDateByIdAsync(dto.ClosedDateId, CurrentOrganizationId, dto.OfficeId);
                if (existingClosedDate == null)
                    return NotFound("Closed date not found");

                var closedDate = dto.ToModel(CurrentOrganizationId);
                var updatedClosedDate = await _accountingRepository.UpdateClosedDateByIdAsync(closedDate);
                return Ok(new ClosedDateResponseDto(updatedClosedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating closed date: {ClosedDateId}", dto.ClosedDateId);
                return ServerError("An error occurred while updating the closed date");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("closed-date/office/{officeId:int}/closedDateId/{closedDateId:int}")]
        public async Task<IActionResult> DeleteClosedDateById(int officeId, int closedDateId)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's closed dates");

            if (closedDateId <= 0)
                return BadRequest("Invalid closed date ID");

            try
            {
                await _accountingRepository.DeleteClosedDateByIdAsync(closedDateId, CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting closed date: {ClosedDateId}", closedDateId);
                return ServerError("An error occurred while deleting the closed date");
            }
        }

        #endregion
    }
}

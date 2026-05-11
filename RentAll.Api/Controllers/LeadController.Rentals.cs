using RentAll.Api.Dtos.Leads.Rentals;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Get

    [HttpGet("rentals")]
    public async Task<IActionResult> GetRentalLeadsAsync()
    {
        try
        {
            var orgAgentIds = await GetAgentIdsForCurrentOrganizationAsync();
            var all = await _leadRepository.GetRentalsAsync();
            var filtered = all.Where(r => !r.AgentId.HasValue || orgAgentIds.Contains(r.AgentId.Value));
            return Ok(filtered.Select(r => new LeadRentalResponseDto(r)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rental leads");
            return ServerError("An error occurred while retrieving rental leads");
        }
    }

    [HttpGet("rentals/{rentalId:int}")]
    public async Task<IActionResult> GetRentalLeadByIdAsync(int rentalId)
    {
        if (rentalId <= 0)
            return BadRequest("RentalId is required");

        try
        {
            var rental = await _leadRepository.GetRentalByIdAsync(rentalId);
            if (rental == null)
                return NotFound("Rental lead not found");

            if (!await CanViewRentalLeadAsync(rental))
                return NotFound("Rental lead not found");

            return Ok(new LeadRentalResponseDto(rental));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rental lead {RentalId}", rentalId);
            return ServerError("An error occurred while retrieving the rental lead");
        }
    }

    #endregion

    #region Post

    [HttpPost("rentals")]
    public async Task<IActionResult> CreateRentalLeadAsync([FromBody] CreateLeadRentalDto dto)
    {
        if (dto == null)
            return BadRequest("Rental lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!await CanAssignAgentAsync(dto.AgentId))
            return BadRequest("AgentId is not valid for the current organization.");

        try
        {
            var created = await _leadRepository.CreateRentalAsync(dto.ToModel());
            return Ok(new LeadRentalResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rental lead");
            return ServerError("An error occurred while creating the rental lead");
        }
    }

    #endregion

    #region Put

    [HttpPut("rentals")]
    public async Task<IActionResult> UpdateRentalLeadAsync([FromBody] UpdateLeadRentalDto dto)
    {
        if (dto == null)
            return BadRequest("Rental lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!await CanAssignAgentAsync(dto.AgentId))
            return BadRequest("AgentId is not valid for the current organization.");

        try
        {
            var existing = await _leadRepository.GetRentalByIdAsync(dto.RentalId);
            if (existing == null)
                return NotFound("Rental lead not found");

            if (!await CanViewRentalLeadAsync(existing))
                return NotFound("Rental lead not found");

            var updated = await _leadRepository.UpdateRentalByIdAsync(dto.ToModel());
            return Ok(new LeadRentalResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rental lead");
            return ServerError("An error occurred while updating the rental lead");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("rentals/{rentalId:int}")]
    public async Task<IActionResult> DeleteRentalLeadAsync(int rentalId)
    {
        if (rentalId <= 0)
            return BadRequest("RentalId is required");

        try
        {
            var existing = await _leadRepository.GetRentalByIdAsync(rentalId);
            if (existing == null)
                return NotFound("Rental lead not found");

            if (!await CanViewRentalLeadAsync(existing))
                return NotFound("Rental lead not found");

            await _leadRepository.DeleteRentalByIdAsync(rentalId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rental lead {RentalId}", rentalId);
            return ServerError("An error occurred while deleting the rental lead");
        }
    }

    #endregion
}

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
            var all = await _leadRepository.GetRentalsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            return Ok(all.Select(r => new LeadRentalResponseDto(r)));
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

            if (rental.OrganizationId != CurrentOrganizationId)
                return NotFound("Rental lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == rental.OfficeId))
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

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (dto.AgentId.HasValue && await _organizationRepository.GetAgentByIdAsync(dto.AgentId.Value, CurrentOrganizationId) == null)
            return BadRequest("AgentId is not valid for the current organization.");

        try
        {
            var created = await _leadRepository.CreateRentalAsync(dto.ToModel(CurrentOrganizationId));
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

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (dto.AgentId.HasValue && await _organizationRepository.GetAgentByIdAsync(dto.AgentId.Value, CurrentOrganizationId) == null)
            return BadRequest("AgentId is not valid for the current organization.");

        try
        {
            var existing = await _leadRepository.GetRentalByIdAsync(dto.RentalId);
            if (existing == null)
                return NotFound("Rental lead not found");

            if (existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Rental lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
                return NotFound("Rental lead not found");

            var updated = dto.ToModel();
            updated.OrganizationId = existing.OrganizationId;
            var updatedResult = await _leadRepository.UpdateRentalByIdAsync(updated);
            return Ok(new LeadRentalResponseDto(updatedResult));
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

            if (existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Rental lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
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

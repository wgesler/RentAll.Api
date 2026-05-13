using RentAll.Api.Dtos.Leads.General;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Get

    [HttpGet("general")]
    public async Task<IActionResult> GetGeneralLeadsAsync()
    {
        try
        {
            var all = await _leadRepository.GetGeneralsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            return Ok(all.Select(g => new LeadGeneralResponseDto(g)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting general leads");
            return ServerError("An error occurred while retrieving general leads");
        }
    }

    [HttpGet("general/{generalId:int}")]
    public async Task<IActionResult> GetGeneralLeadByIdAsync(int generalId)
    {
        if (generalId <= 0)
        {
            return BadRequest("GeneralId is required");
        }

        try
        {
            var lead = await _leadRepository.GetGeneralByIdAsync(generalId);
            if (lead == null)
            {
                return NotFound("General lead not found");
            }

            if (lead.OrganizationId != CurrentOrganizationId)
            {
                return NotFound("General lead not found");
            }

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == lead.OfficeId))
            {
                return NotFound("General lead not found");
            }

            return Ok(new LeadGeneralResponseDto(lead));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting general lead {GeneralId}", generalId);
            return ServerError("An error occurred while retrieving the general lead");
        }
    }

    #endregion

    #region Post

    [HttpPost("general")]
    public async Task<IActionResult> CreateGeneralLeadAsync([FromBody] CreateLeadGeneralDto dto)
    {
        if (dto == null)
        {
            return BadRequest("General lead data is required");
        }

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
        {
            return BadRequest(errorMessage ?? "Invalid request data");
        }

        try
        {
            var created = await _leadRepository.CreateGeneralAsync(dto.ToModel(CurrentOrganizationId));
            return Ok(new LeadGeneralResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating general lead");
            return ServerError("An error occurred while creating the general lead");
        }
    }

    #endregion

    #region Put

    [HttpPut("general")]
    public async Task<IActionResult> UpdateGeneralLeadAsync([FromBody] UpdateLeadGeneralDto dto)
    {
        if (dto == null)
        {
            return BadRequest("General lead data is required");
        }

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
        {
            return BadRequest(errorMessage ?? "Invalid request data");
        }

        try
        {
            var existing = await _leadRepository.GetGeneralByIdAsync(dto.GeneralId);
            if (existing == null)
            {
                return NotFound("General lead not found");
            }

            if (existing.OrganizationId != CurrentOrganizationId)
            {
                return NotFound("General lead not found");
            }

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
            {
                return NotFound("General lead not found");
            }

            var updated = dto.ToModel(existing.OrganizationId);
            var updatedResult = await _leadRepository.UpdateGeneralByIdAsync(updated, existing.OrganizationId, existing.OfficeId);
            return Ok(new LeadGeneralResponseDto(updatedResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating general lead");
            return ServerError("An error occurred while updating the general lead");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("general/{generalId:int}")]
    public async Task<IActionResult> DeleteGeneralLeadAsync(int generalId)
    {
        if (generalId <= 0)
        {
            return BadRequest("GeneralId is required");
        }

        try
        {
            var existing = await _leadRepository.GetGeneralByIdAsync(generalId);
            if (existing == null)
            {
                return NotFound("General lead not found");
            }

            if (existing.OrganizationId != CurrentOrganizationId)
            {
                return NotFound("General lead not found");
            }

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
            {
                return NotFound("General lead not found");
            }

            await _leadRepository.DeleteGeneralByIdAsync(generalId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting general lead {GeneralId}", generalId);
            return ServerError("An error occurred while deleting the general lead");
        }
    }

    #endregion
}

using RentAll.Api.Dtos.Leads.Partners;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Get

    [HttpGet("partners")]
    public async Task<IActionResult> GetPartnerLeadsAsync()
    {
        try
        {
            var all = await _leadRepository.GetPartnersByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            return Ok(all.Select(p => new LeadPartnerResponseDto(p)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner leads");
            return ServerError("An error occurred while retrieving partner leads");
        }
    }

    [HttpGet("partners/{partnerId:int}")]
    public async Task<IActionResult> GetPartnerLeadByIdAsync(int partnerId)
    {
        if (partnerId <= 0)
            return BadRequest("PartnerId is required");

        try
        {
            var partner = await _leadRepository.GetPartnerByIdAsync(partnerId);
            if (partner == null)
                return NotFound("Partner lead not found");

            if (partner.OrganizationId != CurrentOrganizationId)
                return NotFound("Partner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == partner.OfficeId))
                return NotFound("Partner lead not found");

            return Ok(new LeadPartnerResponseDto(partner));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner lead {PartnerId}", partnerId);
            return ServerError("An error occurred while retrieving the partner lead");
        }
    }

    #endregion

    #region Post

    [HttpPost("partners")]
    public async Task<IActionResult> CreatePartnerLeadAsync([FromBody] CreateLeadPartnerDto dto)
    {
        if (dto == null)
            return BadRequest("Partner lead data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var created = await _leadRepository.CreatePartnerAsync(dto.ToModel(CurrentOrganizationId, CurrentUser));
            return Ok(new LeadPartnerResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partner lead");
            return ServerError("An error occurred while creating the partner lead");
        }
    }

    #endregion

    #region Put

    [HttpPut("partners")]
    public async Task<IActionResult> UpdatePartnerLeadAsync([FromBody] UpdateLeadPartnerDto dto)
    {
        if (dto == null)
            return BadRequest("Partner lead data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _leadRepository.GetPartnerByIdAsync(dto.PartnerId);
            if (existing == null)
                return NotFound("Partner lead not found");

            if (existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Partner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
                return NotFound("Partner lead not found");

            var updated = dto.ToModel(existing.OrganizationId, CurrentUser);
            var updatedResult = await _leadRepository.UpdatePartnerByIdAsync(updated);
            return Ok(new LeadPartnerResponseDto(updatedResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating partner lead");
            return ServerError("An error occurred while updating the partner lead");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("partners/{partnerId:int}")]
    public async Task<IActionResult> DeletePartnerLeadAsync(int partnerId)
    {
        if (partnerId <= 0)
            return BadRequest("PartnerId is required");

        try
        {
            var existing = await _leadRepository.GetPartnerByIdAsync(partnerId);
            if (existing == null)
                return NotFound("Partner lead not found");

            if (existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Partner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
                return NotFound("Partner lead not found");

            await _leadRepository.DeletePartnerByIdAsync(partnerId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting partner lead {PartnerId}", partnerId);
            return ServerError("An error occurred while deleting the partner lead");
        }
    }

    #endregion
}

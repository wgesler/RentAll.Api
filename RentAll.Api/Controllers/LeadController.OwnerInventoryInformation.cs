using RentAll.Api.Dtos.Leads.Owners;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Select

    [HttpGet("owners/inventory-information/{ownerId:int}")]
    public async Task<IActionResult> GetOwnerInventoryInformationByOwnerIdAsync(int ownerId)
    {
        if (ownerId <= 0)
            return BadRequest("OwnerId is required");

        try
        {
            var owner = await _leadRepository.GetOwnerByIdAsync(ownerId);
            if (owner == null || owner.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == owner.OfficeId))
                return NotFound("Owner lead not found");

            var inventoryInformation = await _leadRepository.GetOwnerInventoryInformationByOwnerIdAsync(ownerId, CurrentOrganizationId);
            if (inventoryInformation == null)
                return Ok();

            return Ok(new OwnerInventoryInformationResponseDto(inventoryInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner inventory information by owner ID: {OwnerId}", ownerId);
            return ServerError("An error occurred while retrieving owner inventory information");
        }
    }

    #endregion

    #region Create

    [HttpPost("owners/inventory-information")]
    public async Task<IActionResult> CreateOwnerInventoryInformationAsync([FromBody] CreateOwnerInventoryInformationDto dto)
    {
        if (dto == null)
            return BadRequest("Owner inventory information data is required");

        dto.OrganizationId = CurrentOrganizationId;

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var owner = await _leadRepository.GetOwnerByIdAsync(dto.OwnerId);
            if (owner == null || owner.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == owner.OfficeId))
                return NotFound("Owner lead not found");

            var inventoryInformation = dto.ToModel(CurrentUser);
            var createdInventoryInformation = await _leadRepository.CreateOwnerInventoryInformationAsync(inventoryInformation);
            return Ok(new OwnerInventoryInformationResponseDto(createdInventoryInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner inventory information");
            return ServerError("An error occurred while creating owner inventory information");
        }
    }

    #endregion

    #region Update

    [HttpPut("owners/inventory-information")]
    public async Task<IActionResult> UpdateOwnerInventoryInformationAsync([FromBody] UpdateOwnerInventoryInformationDto dto)
    {
        if (dto == null)
            return BadRequest("Owner inventory information data is required");

        dto.OrganizationId = CurrentOrganizationId;

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var owner = await _leadRepository.GetOwnerByIdAsync(dto.OwnerId);
            if (owner == null || owner.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == owner.OfficeId))
                return NotFound("Owner lead not found");

            var inventoryInformation = dto.ToModel(CurrentUser);

            var existing = await _leadRepository.GetOwnerInventoryInformationByOwnerIdAsync(dto.OwnerId, CurrentOrganizationId);
            if (existing == null)
            {
                var addedInventoryInformation = await _leadRepository.CreateOwnerInventoryInformationAsync(inventoryInformation);
                return Ok(new OwnerInventoryInformationResponseDto(addedInventoryInformation));
            }

            var updatedInventoryInformation = await _leadRepository.UpdateOwnerInventoryInformationByIdAsync(inventoryInformation);
            return Ok(new OwnerInventoryInformationResponseDto(updatedInventoryInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating owner inventory information: {OwnerId}", dto.OwnerId);
            return ServerError("An error occurred while updating owner inventory information");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("owners/inventory-information/{ownerId:int}")]
    public async Task<IActionResult> DeleteOwnerInventoryInformationByOwnerIdAsync(int ownerId)
    {
        if (ownerId <= 0)
            return BadRequest("OwnerId is required");

        try
        {
            var owner = await _leadRepository.GetOwnerByIdAsync(ownerId);
            if (owner == null || owner.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == owner.OfficeId))
                return NotFound("Owner lead not found");

            var existing = await _leadRepository.GetOwnerInventoryInformationByOwnerIdAsync(ownerId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Owner inventory information not found");

            await _leadRepository.DeleteOwnerInventoryInformationByIdAsync(ownerId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting owner inventory information: {OwnerId}", ownerId);
            return ServerError("An error occurred while deleting owner inventory information");
        }
    }

    #endregion
}

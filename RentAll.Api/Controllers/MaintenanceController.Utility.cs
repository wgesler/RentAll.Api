using RentAll.Api.Dtos.Maintenances.Utilities;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("utility/{propertyId:guid}")]
    public async Task<IActionResult> GetUtilitiesByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var records = await _maintenanceRepository.GetUtilitiesByPropertyIdAsync(propertyId);
            var response = records.Select(o => new UtilityResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting utilities for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving utilities");
        }
    }

    [HttpGet("utility/{utilityId:int}")]
    public async Task<IActionResult> GetUtilityById(int utilityId)
    {
        if (utilityId <= 0)
            return BadRequest("UtilityId is required");

        try
        {
            var record = await _maintenanceRepository.GetUtilityByIdAsync(utilityId);
            if (record == null)
                return NotFound("Utility not found");

            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(record.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Utility not found");

            return Ok(new UtilityResponseDto(record));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting utility by ID: {UtilityId}", utilityId);
            return ServerError("An error occurred while retrieving utility");
        }
    }
    #endregion

    #region Post
    [HttpPost("utility")]
    public async Task<IActionResult> CreateUtility([FromBody] CreateUtilityDto dto)
    {
        if (dto == null)
            return BadRequest("Utility data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var created = await _maintenanceRepository.CreateUtilityAsync(dto.ToModel());
            return Ok(new UtilityResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating utility");
            return ServerError("An error occurred while creating utility");
        }
    }
    #endregion

    #region Put
    [HttpPut("utility")]
    public async Task<IActionResult> UpdateUtility([FromBody] UpdateUtilityDto dto)
    {
        if (dto == null)
            return BadRequest("Utility data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var existing = await _maintenanceRepository.GetUtilityByIdAsync(dto.UtilityId);
            if (existing == null)
                return NotFound("Utility not found");

            var updated = await _maintenanceRepository.UpdateUtilityAsync(dto.ToModel());
            return Ok(new UtilityResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating utility: {UtilityId}", dto.UtilityId);
            return ServerError("An error occurred while updating utility");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("utility/{utilityId:int}")]
    public async Task<IActionResult> DeleteUtilityByIdAsync(int utilityId)
    {
        if (utilityId <= 0)
            return BadRequest("UtilityId is required");

        try
        {
            await _maintenanceRepository.DeleteUtilityByIdAsync(utilityId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting utility: {UtilityId}", utilityId);
            return ServerError("An error occurred while deleting utility");
        }
    }
    #endregion
}

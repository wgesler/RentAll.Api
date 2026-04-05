using RentAll.Api.Dtos.Maintenances.Appliances;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("appliance/{propertyId:guid}")]
    public async Task<IActionResult> GetAppliancesByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _maintenanceRepository.GetAppliancesByPropertyIdAsync(propertyId);
            var response = records.Select(o => new ApplianceResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appliances for maintenance: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving appliances");
        }
    }

    [HttpGet("appliance/{applianceId:int}")]
    public async Task<IActionResult> GetApplianceById(int applianceId)
    {
        if (applianceId <= 0)
            return BadRequest("ApplianceId is required");

        try
        {
            var record = await _maintenanceRepository.GetApplianceByIdAsync(applianceId);
            if (record == null)
                return NotFound("Appliance not found");

            return Ok(new ApplianceResponseDto(record));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appliance by ID: {ApplianceId}", applianceId);
            return ServerError("An error occurred while retrieving appliance");
        }
    }
    #endregion

    #region Post
    [HttpPost("appliance")]
    public async Task<IActionResult> CreateAppliance([FromBody] CreateApplianceDto dto)
    {
        if (dto == null)
            return BadRequest("Appliance data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var appliance = dto.ToModel();
            var created = await _maintenanceRepository.CreateApplianceAsync(appliance);
            return Ok(new ApplianceResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appliance");
            return ServerError("An error occurred while creating appliance");
        }
    }
    #endregion

    #region Put
    [HttpPut("appliance")]
    public async Task<IActionResult> UpdateAppliance([FromBody] UpdateApplianceDto dto)
    {
        if (dto == null)
            return BadRequest("Appliance data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var existing = await _maintenanceRepository.GetApplianceByIdAsync(dto.ApplianceId);
            if (existing == null)
                return NotFound("Appliance not found");

            var appliance = dto.ToModel();
            var updated = await _maintenanceRepository.UpdateApplianceAsync(appliance);
            return Ok(new ApplianceResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appliance: {ApplianceId}", dto.ApplianceId);
            return ServerError("An error occurred while updating appliance");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("appliance/{applianceId:int}")]
    public async Task<IActionResult> DeleteApplianceByIdAsync(int applianceId)
    {
        if (applianceId <= 0)
            return BadRequest("ApplianceId is required");

        try
        {
            await _maintenanceRepository.DeleteApplianceByIdAsync(applianceId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appliance: {ApplianceId}", applianceId);
            return ServerError("An error occurred while deleting appliance");
        }
    }
    #endregion
}

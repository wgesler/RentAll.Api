using RentAll.Api.Dtos.Maintenances.Maintenances;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetMaintenanceByPropertyIdAsync(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var record = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(propertyId, CurrentOrganizationId);
            if (record == null || record.IsDeleted)
                return Ok(); // This is not an error, the UI will default to blank form

            var response = new MaintenanceResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance records for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving maintenance records");
        }
    }

    [HttpGet("{maintenanceId:guid}/property/{propertyId:guid}")]
    public async Task<IActionResult> GetMaintenanceByIdAsync(Guid maintenanceId, Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var record = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(propertyId, CurrentOrganizationId, maintenanceId);
            if (record == null || record.IsDeleted)
                return NotFound("Maintenance record not found");

            var response = new MaintenanceResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance records for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving maintenance records");
        }
    }

    [HttpGet("{maintenanceId:guid}")]
    public async Task<IActionResult> GetMaintenanceByIdAsync(Guid maintenanceId)
    {
        if (maintenanceId == Guid.Empty)
            return BadRequest("MaintenanceId is required");

        try
        {
            var record = await _maintenanceRepository.GetMaintenanceByIdAsync(maintenanceId, CurrentOrganizationId);
            if (record == null || record.IsDeleted)
                return NotFound("Maintenance record not found");

            var response = new MaintenanceResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance record by ID: {MaintenanceId}", maintenanceId);
            return ServerError("An error occurred while retrieving the maintenance record");
        }
    }

    #endregion

    #region Post

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceDto dto)
    {
        if (dto == null)
            return BadRequest("Maintenance data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId, CurrentOrganizationId);
            if (property == null)
                return NotFound("Property not found");

            var model = dto.ToModel(CurrentUser);
            var created = await _maintenanceRepository.CreateAsync(model);

            var response = new MaintenanceResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance record");
            return ServerError("An error occurred while creating the maintenance record");
        }
    }

    #endregion

    #region Put

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMaintenanceDto dto)
    {
        if (dto == null)
            return BadRequest("Maintenance data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var model = dto.ToModel(CurrentUser);
            var updated = await _maintenanceManager.UpdateByIdAsync(model, CurrentOfficeAccess);

            var response = new MaintenanceResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance record: {MaintenanceId}", dto.MaintenanceId);
            return ServerError("An error occurred while updating the maintenance record");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("{maintenanceId:guid}")]
    public async Task<IActionResult> Delete(Guid maintenanceId, Guid propertyId)
    {
        if (maintenanceId == Guid.Empty)
            return BadRequest("MaintenanceId is required");

        try
        {
            await _maintenanceRepository.DeleteMaintenanceByIdAsync(maintenanceId, propertyId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance record: {MaintenanceId}", maintenanceId);
            return ServerError("An error occurred while deleting the maintenance record");
        }
    }

    #endregion
}

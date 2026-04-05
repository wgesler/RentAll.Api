using RentAll.Api.Dtos.Maintenances.MaintenanceItems;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("maintenance-item/{propertyId:guid}")]
    public async Task<IActionResult> GetMaintenanceItemsByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _maintenanceRepository.GetMaintenanceItemsByPropertyIdAsync(propertyId);
            var response = records.Select(o => new MaintenanceItemResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance items for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving maintenance items");
        }
    }

    [HttpGet("maintenance-item/{maintenanceItemId:int}")]
    public async Task<IActionResult> GetMaintenanceItemById(int maintenanceItemId)
    {
        if (maintenanceItemId <= 0)
            return BadRequest("MaintenanceItemId is required");

        try
        {
            var record = await _maintenanceRepository.GetMaintenanceItemByIdAsync(maintenanceItemId);
            if (record == null)
                return NotFound("Maintenance item not found");

            return Ok(new MaintenanceItemResponseDto(record));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance item by id: {MaintenanceItemId}", maintenanceItemId);
            return ServerError("An error occurred while retrieving maintenance item");
        }
    }
    #endregion

    #region Post
    [HttpPost("maintenance-item")]
    public async Task<IActionResult> CreateMaintenanceItem([FromBody] CreateMaintenanceItemDto dto)
    {
        if (dto == null)
            return BadRequest("Maintenance item data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var created = await _maintenanceRepository.CreateMaintenanceItemAsync(dto.ToModel());
            return Ok(new MaintenanceItemResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance item for property: {PropertyId}", dto.PropertyId);
            return ServerError("An error occurred while creating maintenance item");
        }
    }
    #endregion

    #region Put
    [HttpPut("maintenance-item")]
    public async Task<IActionResult> UpdateMaintenanceItem([FromBody] UpdateMaintenanceItemDto dto)
    {
        if (dto == null)
            return BadRequest("Maintenance item data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (maintenance == null || maintenance.IsDeleted)
                return NotFound("Property maintenance record not found");

            var existing = await _maintenanceRepository.GetMaintenanceItemByIdAsync(dto.MaintenanceItemId);
            if (existing == null)
                return NotFound("Maintenance item not found");

            var updated = await _maintenanceRepository.UpdateMaintenanceItemAsync(dto.ToModel());
            return Ok(new MaintenanceItemResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance item by id: {MaintenanceItemId}", dto.MaintenanceItemId);
            return ServerError("An error occurred while updating maintenance item");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("maintenance-item/{maintenanceItemId:int}")]
    public async Task<IActionResult> DeleteMaintenanceItemByIdAsync(int maintenanceItemId)
    {
        if (maintenanceItemId <= 0)
            return BadRequest("MaintenanceItemId is required");

        try
        {
            await _maintenanceRepository.DeleteMaintenanceItemByIdAsync(maintenanceItemId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance item by id: {MaintenanceItemId}", maintenanceItemId);
            return ServerError("An error occurred while deleting maintenance item");
        }
    }
    #endregion
}

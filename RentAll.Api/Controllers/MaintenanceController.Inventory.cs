using RentAll.Api.Dtos.Maintenances.Inventories;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get

    [HttpGet("inventory/property/{propertyId:guid}")]
    public async Task<IActionResult> GetInventoriesByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _maintenanceRepository.GetInventoriesByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new InventoryResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory records for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving inventory records");
        }
    }

    [HttpGet("inventory/latest/{propertyId:guid}")]
    public async Task<IActionResult> GetLatestInventoryByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var record = await _maintenanceRepository.GetLatestInventoryByPropertyId(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            if (record == null)
                return NotFound("Inventory record not found");

            var response = new InventoryResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest inventory record for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving the latest inventory record");
        }
    }

    [HttpGet("inventory/{inventoryId:int}")]
    public async Task<IActionResult> GetInventoryById(int inventoryId)
    {
        if (inventoryId <= 0)
            return BadRequest("InventoryId is required");

        try
        {
            var record = await _maintenanceRepository.GetInventoryByIdAsync(inventoryId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Inventory record not found");

            var response = new InventoryResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory record by ID: {InventoryId}", inventoryId);
            return ServerError("An error occurred while retrieving the inventory record");
        }
    }

    #endregion

    #region Post

    [HttpPost("inventory")]
    public async Task<IActionResult> CreateInventory([FromBody] CreateInventoryDto dto)
    {
        if (dto == null)
            return BadRequest("Inventory data is required");

        if(dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            if (await _maintenanceManager.CurrentInventoryAlreadyExistsForProperty(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess))
                return BadRequest("An active inventory record already exists for this property");

            var model = dto.ToModel(CurrentUser);
            var created = await _maintenanceRepository.CreateInventoryAsync(model);

            var response = new InventoryResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory record");
            return ServerError("An error occurred while creating the inventory record");
        }
    }

    #endregion

    #region Put

    [HttpPut("inventory")]
    public async Task<IActionResult> UpdateInventory([FromBody] UpdateInventoryDto dto)
    {
        if (dto == null)
            return BadRequest("Inventory data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetMaintenanceByIdAsync(dto.MaintenanceId, CurrentOrganizationId);
            if (maintenance == null || !maintenance.IsActive)
                return NotFound("Maintenance record not valid");

            var existing = await _maintenanceRepository.GetInventoryByIdAsync(dto.InventoryId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Inventory record not found");

            var model = dto.ToModel(CurrentUser);
            var updated = await _maintenanceRepository.UpdateInventoryAsync(model);

            var response = new InventoryResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory record: {InventoryId}", dto.InventoryId);
            return ServerError("An error occurred while updating the inventory record");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("inventory/{inventoryId:int}")]
    public async Task<IActionResult> DeleteInventory(int inventoryId)
    {
        if (inventoryId <= 0)
            return BadRequest("InventoryId is required");

        try
        {
            await _maintenanceRepository.DeleteInventoryByIdAsync(inventoryId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory record: {InventoryId}", inventoryId);
            return ServerError("An error occurred while deleting the inventory record");
        }
    }

    #endregion
}

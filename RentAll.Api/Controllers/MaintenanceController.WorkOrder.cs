using RentAll.Api.Dtos.Maintenances.WorkOrders;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("work-order")]
    public async Task<IActionResult> GetAllWorkOrders()
    {
        try
        {
            var records = await _maintenanceRepository.GetWorkOrdersByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new WorkOrderResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting work orders");
            return ServerError("An error occurred while retrieving work orders");
        }
    }

    [HttpGet("work-order/property/{propertyId:guid}")]
    public async Task<IActionResult> GetWorkOrdersByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _maintenanceRepository.GetWorkOrdersByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new WorkOrderResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting work orders for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving work orders");
        }
    }

    [HttpGet("work-order/{workOrderId:int}")]
    public async Task<IActionResult> GetWorkOrderById(int workOrderId)
    {
        if (workOrderId <= 0)
            return BadRequest("WorkOrderId is required");

        try
        {
            var record = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Work order record not found");

            var response = new WorkOrderResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting work order by ID: {WorkOrderId}", workOrderId);
            return ServerError("An error occurred while retrieving the work order");
        }
    }
    #endregion

    #region Post
    [HttpPost("work-order")]
    public async Task<IActionResult> CreateWorkOrder([FromBody] CreateWorkOrderDto dto)
    {
        if (dto == null)
            return BadRequest("Work order data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var model = dto.ToModel();
            var created = await _maintenanceRepository.CreateWorkOrderAsync(model);

            var response = new WorkOrderResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work order");
            return ServerError("An error occurred while creating the work order");
        }
    }
    #endregion

    #region Put
    [HttpPut("work-order")]
    public async Task<IActionResult> UpdateWorkOrder([FromBody] UpdateWorkOrderDto dto)
    {
        if (dto == null)
            return BadRequest("Work order data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _maintenanceRepository.GetWorkOrderByIdAsync(dto.WorkOrderId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Work order record not found");

            var model = dto.ToModel();
            var updated = await _maintenanceRepository.UpdateWorkOrderAsync(model);

            var response = new WorkOrderResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating work order: {WorkOrderId}", dto.WorkOrderId);
            return ServerError("An error occurred while updating the work order");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("work-order/{workOrderId:int}")]
    public async Task<IActionResult> DeleteWorkOrderByIdAsync(int workOrderId)
    {
        if (workOrderId <= 0)
            return BadRequest("WorkOrderId is required");

        try
        {
            await _maintenanceRepository.DeleteWorkOrderByIdAsync(workOrderId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting work order: {WorkOrderId}", workOrderId);
            return ServerError("An error occurred while deleting the work order");
        }
    }

    #endregion
}

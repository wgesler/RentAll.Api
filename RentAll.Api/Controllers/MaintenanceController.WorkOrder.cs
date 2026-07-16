using RentAll.Api.Dtos.Maintenances.WorkOrders;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpPost("work-order/search")]
    public async Task<IActionResult> SearchWorkOrders([FromBody] GetWorkOrdersDto dto)
    {
        if (dto == null)
            return BadRequest("Work order search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var records = await _maintenanceRepository.GetWorkOrdersByCriteriaAsync(criteria);
            var response = records.Select(o => new WorkOrderResponseDto(o)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching work orders");
            return ServerError("An error occurred while retrieving work orders");
        }
    }

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

    [HttpGet("work-order/office/{officeId:int}")]
    public async Task<IActionResult> GetWorkOrdersByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _maintenanceRepository.GetWorkOrdersByOfficeIdsAsync(CurrentOrganizationId, officeAccess);
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

    [HttpGet("work-order/{workOrderId:guid}")]
    public async Task<IActionResult> GetWorkOrderById(Guid workOrderId)
    {
        if (workOrderId == Guid.Empty)
            return BadRequest("WorkOrderId is required");

        try
        {
            var record = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Work order record not found");

            return Ok(new WorkOrderResponseDto(record));
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

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var workOrder = dto.ToModel(CurrentUser);
            var created = await _maintenanceRepository.CreateWorkOrderAsync(workOrder);

            await _accountingManager.CreateJournalEntryFromWorkOrderAsync(created, CurrentUser);

            return Ok(new WorkOrderResponseDto(created));
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

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _maintenanceRepository.GetWorkOrderByIdAsync(dto.WorkOrderId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Work order record not found");

            var hardClosedResult = RefuseIfJournalEntryHardClosed(existing.PostingStatusId, "work order");
            if (hardClosedResult != null)
                return hardClosedResult;

            var workOrder = dto.ToModel(CurrentUser);

            var updated = await _accountingManager.UpdateWorkOrderAsync(workOrder, CurrentUser);

            return Ok(new WorkOrderResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating work order: {WorkOrderId}", dto.WorkOrderId);
            return ServerError("An error occurred while updating the work order");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("work-order/{workOrderId:guid}")]
    public async Task<IActionResult> DeleteWorkOrderByIdAsync(Guid workOrderId)
    {
        if (workOrderId == Guid.Empty)
            return BadRequest("WorkOrderId is required");

        try
        {
            await _maintenanceRepository.DeleteWorkOrderByIdAsync(workOrderId, CurrentOrganizationId, CurrentUser);
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

using RentAll.Api.Dtos.Maintenances.Inspections;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get

    [HttpGet("inspection/property/{propertyId:guid}")]
    public async Task<IActionResult> GetInspectionsByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _maintenanceRepository.GetInspectionsByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new InspectionResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inspection records for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving inspection records");
        }
    }

    [HttpGet("inspection/{inspectionId:int}")]
    public async Task<IActionResult> GetInspectionById(int inspectionId)
    {
        if (inspectionId <= 0)
            return BadRequest("InspectionId is required");

        try
        {
            var record = await _maintenanceRepository.GetInspectionByIdAsync(inspectionId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Inspection record not found");

            var response = new InspectionResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inspection record by ID: {InspectionId}", inspectionId);
            return ServerError("An error occurred while retrieving the inspection record");
        }
    }

    #endregion

    #region Post

    [HttpPost("inspection")]
    public async Task<IActionResult> CreateInspection([FromBody] CreateInspectionDto dto)
    {
        if (dto == null)
            return BadRequest("Inspection data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            if (await _maintenanceManager.CurrentInspectionAlreadyExistsForProperty(dto.PropertyId, CurrentOrganizationId, CurrentOfficeAccess))
                return BadRequest("An active inspection record already exists for this property");

            var model = dto.ToModel(CurrentUser);
            var created = await _maintenanceRepository.CreateInspectionAsync(model);

            var response = new InspectionResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inspection record");
            return ServerError("An error occurred while creating the inspection record");
        }
    }

    #endregion

    #region Put

    [HttpPut("inspection")]
    public async Task<IActionResult> UpdateInspection([FromBody] UpdateInspectionDto dto)
    {
        if (dto == null)
            return BadRequest("Inspection data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var maintenance = await _maintenanceRepository.GetByIdAsync(dto.MaintenanceId, CurrentOrganizationId);
            if (maintenance == null || !maintenance.IsActive)
                return NotFound("Maintenance record not valid");

            var existing = await _maintenanceRepository.GetInspectionByIdAsync(dto.InspectionId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Inspection record not found");

            var model = dto.ToModel(CurrentUser);
            var updated = await _maintenanceRepository.UpdateInspectionByIdAsync(model);

            var response = new InspectionResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inspection record: {InspectionId}", dto.InspectionId);
            return ServerError("An error occurred while updating the inspection record");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("inspection/{inspectionId:int}")]
    public async Task<IActionResult> DeleteInspection(int inspectionId)
    {
        if (inspectionId <= 0)
            return BadRequest("InspectionId is required");

        try
        {
            var existing = await _maintenanceRepository.GetInspectionByIdAsync(inspectionId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Inspection record not found");

            await _maintenanceRepository.DeleteInspectionByIdAsync(inspectionId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inspection record: {InspectionId}", inspectionId);
            return ServerError("An error occurred while deleting the inspection record");
        }
    }

    #endregion
}

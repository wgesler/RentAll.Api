using RentAll.Api.Dtos.Maintenances.Contractors;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("contractor")]
    public async Task<IActionResult> GetContractorsByOfficeIdsAsync()
    {
        try
        {
            var records = await _maintenanceRepository.GetContractorsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new ContractorResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contractors");
            return ServerError("An error occurred while retrieving contractors");
        }
    }

    [HttpGet("contractor/{contractorId:guid}")]
    public async Task<IActionResult> GetContractorByIdAsync(Guid contractorId)
    {
        if (contractorId == Guid.Empty)
            return BadRequest("contractorId is required");

        try
        {
            var record = await _maintenanceRepository.GetContractorByIdAsync(contractorId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Contractor record not found");

            var response = new ContractorResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contractor by ID: {contractorId}", contractorId);
            return ServerError("An error occurred while retrieving the contractor");
        }
    }
    #endregion

    #region Post
    [HttpPost("contractor")]
    public async Task<IActionResult> CreateContractorAsync([FromBody] CreateContractorDto dto)
    {
        if (dto == null)
            return BadRequest("Contractor data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            // Get a new Contact code
            var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Contractor);
            var contractor = dto.ToModel(code, CurrentUser);
            var created = await _maintenanceRepository.CreateContractorAsync(contractor);

            var response = new ContractorResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contractor");
            return ServerError("An error occurred while creating the contractor");
        }
    }
    #endregion

    #region Put
    [HttpPut("contractor")]
    public async Task<IActionResult> UpdateContractorAsync([FromBody] UpdateContractorDto dto)
    {
        if (dto == null)
            return BadRequest("Contractor data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _maintenanceRepository.GetContractorByIdAsync(dto.ContractorId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Contractor record not found");

            var model = dto.ToModel(CurrentUser);
            var updated = await _maintenanceRepository.UpdateContractorAsync(model);

            var response = new ContractorResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contractor: {ContractorId}", dto.ContractorId);
            return ServerError("An error occurred while updating the contractor");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("contractor/{contractorId:guid}")]
    public async Task<IActionResult> DeleteContractorByIdAsnyc(Guid contractorId)
    {
        if (contractorId == Guid.Empty)
            return BadRequest("contractorId is required");

        try
        {
            await _maintenanceRepository.DeleteContractorByIdAsync(contractorId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contractor: {contractorId}", contractorId);
            return ServerError("An error occurred while deleting the contractor");
        }
    }

    #endregion
}

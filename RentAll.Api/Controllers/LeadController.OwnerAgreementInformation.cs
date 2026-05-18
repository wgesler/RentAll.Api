namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Select

    [HttpGet("owners/agreement-information/{ownerAgreementInformationId:guid}")]
    public async Task<IActionResult> GetOwnerAgreementInformationByIdAsync(Guid ownerAgreementInformationId)
    {
        if (ownerAgreementInformationId == Guid.Empty)
            return BadRequest("OwnerAgreementInformationId is required");

        try
        {
            var agreementInformation = await _leadRepository.GetOwnerAgreementInformationByIdAsync(ownerAgreementInformationId, CurrentOrganizationId);
            if (agreementInformation == null)
                return NotFound("Owner agreement information not found");

            return Ok(new OwnerAgreementInformationResponseDto(agreementInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner agreement information by ID: {OwnerAgreementInformationId}", ownerAgreementInformationId);
            return ServerError("An error occurred while retrieving owner agreement information");
        }
    }

    [HttpGet("owners/agreement-information/scope")]
    public async Task<IActionResult> GetOwnerAgreementInformationByScopeAsync([FromQuery] int? officeId = null, [FromQuery] Guid? propertyId = null)
    {
        try
        {
            if (officeId.HasValue && !CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId.Value))
                return NotFound("Office not found");

            var agreementInformation = await _leadRepository.GetOwnerAgreementInformationByScopeAsync(CurrentOrganizationId, officeId, propertyId);
            if (agreementInformation == null)
                return Ok();

            return Ok(new OwnerAgreementInformationResponseDto(agreementInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner agreement information by scope: {OrganizationId}-{OfficeId}-{PropertyId}", CurrentOrganizationId, officeId, propertyId);
            return ServerError("An error occurred while retrieving owner agreement information");
        }
    }

    #endregion

    #region Create

    [HttpPost("owners/agreement-information")]
    public async Task<IActionResult> CreateOwnerAgreementInformationAsync([FromBody] CreateOwnerAgreementInformationDto dto)
    {
        if (dto == null)
            return BadRequest("Owner agreement information data is required");

        dto.OrganizationId = CurrentOrganizationId;

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (dto.OfficeId.HasValue && !CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == dto.OfficeId.Value))
            return BadRequest("Unauthorized");

        try
        {
            var agreementInformation = dto.ToModel(CurrentUser);
            var createdAgreementInformation = await _leadRepository.CreateOwnerAgreementInformationAsync(agreementInformation);
            return Ok(new OwnerAgreementInformationResponseDto(createdAgreementInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner agreement information");
            return ServerError("An error occurred while creating owner agreement information");
        }
    }

    #endregion

    #region Update

    [HttpPut("owners/agreement-information")]
    public async Task<IActionResult> UpdateOwnerAgreementInformationAsync([FromBody] UpdateOwnerAgreementInformationDto dto)
    {
        if (dto == null)
            return BadRequest("Owner agreement information data is required");

        dto.OrganizationId = CurrentOrganizationId;

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (dto.OfficeId.HasValue && !CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == dto.OfficeId.Value))
            return BadRequest("Unauthorized");

        try
        {
            var agreementInformation = dto.ToModel(CurrentUser);

            var existing = await _leadRepository.GetOwnerAgreementInformationByExactScopeAsync(CurrentOrganizationId, dto.OfficeId, dto.PropertyId);
            if (existing == null)
            {
                var addedAgreementInformation = await _leadRepository.CreateOwnerAgreementInformationAsync(agreementInformation);
                return Ok(new OwnerAgreementInformationResponseDto(addedAgreementInformation));
            }

            var updatedAgreementInformation = await _leadRepository.UpdateOwnerAgreementInformationByIdAsync(agreementInformation);
            return Ok(new OwnerAgreementInformationResponseDto(updatedAgreementInformation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating owner agreement information: {OrganizationId}-{OfficeId}-{PropertyId}", dto.OrganizationId, dto.OfficeId, dto.PropertyId);
            return ServerError("An error occurred while updating owner agreement information");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("owners/agreement-information/{ownerAgreementInformationId:guid}")]
    public async Task<IActionResult> DeleteOwnerAgreementInformationByIdAsync(Guid ownerAgreementInformationId)
    {
        if (ownerAgreementInformationId == Guid.Empty)
            return BadRequest("OwnerAgreementInformationId is required");

        try
        {
            var existing = await _leadRepository.GetOwnerAgreementInformationByIdAsync(ownerAgreementInformationId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Owner agreement information not found");

            await _leadRepository.DeleteOwnerAgreementInformationByIdAsync(ownerAgreementInformationId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting owner agreement information: {OwnerAgreementInformationId}", ownerAgreementInformationId);
            return ServerError("An error occurred while deleting owner agreement information");
        }
    }

    #endregion
}

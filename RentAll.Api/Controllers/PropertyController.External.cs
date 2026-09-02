using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    private static readonly Guid ExternalPropertySystemUserId = new("99999999-9999-9999-9999-999999999999");

    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> CreateExternalProperty([FromBody] CreateExternalPropertyDto dto)
    {
        if (dto == null)
            return BadRequest("Property data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var accessError = await ValidateExternalPropertyAccessAsync(dto.OrganizationId, dto.OfficeId);
        if (accessError != null)
            return accessError;

        try
        {
            var propertyCode = dto.PropertyCode.Trim();
            var existingProperty = await _propertyRepository.GetPropertyByCodeAsync(propertyCode, dto.OrganizationId);
            if (existingProperty != null)
                return Conflict("Property Code already exists");

            var propertyDto = dto.ToCreatePropertyDto(propertyCode);
            var (propertyDtoIsValid, propertyDtoErrorMessage) = propertyDto.IsValid();
            if (!propertyDtoIsValid)
                return BadRequest(propertyDtoErrorMessage ?? "Invalid request data");

            var createdProperty = await _propertyRepository.CreateAsync(propertyDto.ToModel(ExternalPropertySystemUserId));
            await _externalPropertyUploadLogService.LogPropertyCreatedAsync(
                dto.OrganizationId,
                dto.OfficeId,
                dto.VendorId,
                createdProperty.PropertyId,
                createdProperty.PropertyCode);
            return Ok(new PropertyResponseDto(createdProperty));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating external property intake request. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}",
                dto.OrganizationId,
                dto.OfficeId,
                dto.PropertyCode,
                dto.VendorId);
            return ServerError("An error occurred while saving the property");
        }
    }

    [AllowAnonymous]
    [HttpPut("external")]
    public async Task<IActionResult> UpdateExternalProperty([FromBody] JsonElement body)
    {
        var (keysParsed, keys, keysError) = ExternalPropertyPatchMerger.TryParseRequiredKeys(body);
        if (!keysParsed || keys == null)
            return BadRequest(keysError ?? "Invalid request data");

        var accessError = await ValidateExternalPropertyAccessAsync(keys.OrganizationId, keys.OfficeId);
        if (accessError != null)
            return accessError;

        try
        {
            var existingProperty = await _propertyRepository.GetPropertyByCodeAsync(keys.PropertyCode, keys.OrganizationId);
            if (existingProperty == null)
                return NotFound("Property not found");

            var (merged, updateDto, mergeError) = ExternalPropertyPatchMerger.TryMerge(existingProperty, body, keys);
            if (!merged || updateDto == null)
                return BadRequest(mergeError ?? "Invalid request data");

            var (updateResult, updateError) = await TryUpdateExternalPropertyAsync(existingProperty, updateDto);
            if (updateResult == null)
                return BadRequest(updateError ?? "Invalid request data");

            await _externalPropertyUploadLogService.LogPropertyUpdatedAsync(
                keys.OrganizationId,
                keys.OfficeId,
                keys.VendorId,
                updateResult.PropertyId,
                updateResult.PropertyCode);

            return Ok(new PropertyResponseDto(updateResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating external property intake request. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}",
                keys.OrganizationId,
                keys.OfficeId,
                keys.PropertyCode,
                keys.VendorId);
            return ServerError("An error occurred while saving the property");
        }
    }

    private async Task<IActionResult?> ValidateExternalPropertyAccessAsync(Guid organizationId, int officeId)
    {
        var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalPropertyKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        SetApplicationLogContext(organizationId, officeId);

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        if (office == null)
            return BadRequest("Invalid OfficeId for OrganizationId");

        return null;
    }

    private async Task<(Property? Property, string? ErrorMessage)> TryUpdateExternalPropertyAsync(Property existingProperty, UpdatePropertyDto updateDto)
    {
        var (updateIsValid, updateErrorMessage) = updateDto.IsValid();
        if (!updateIsValid)
            return (null, updateErrorMessage ?? "Invalid request data");

        var property = updateDto.ToModel(ExternalPropertySystemUserId);
        if (existingProperty.OfficeId != updateDto.OfficeId)
            await _propertyManager.UpdatePropertyOfficeAsync(property, ExternalPropertySystemUserId);

        var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
        return (updatedProperty, null);
    }
}

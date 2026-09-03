using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
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

        var attempt = new ExternalPropertyApiAttemptLog
        {
            OrganizationId = dto.OrganizationId,
            OfficeId = dto.OfficeId,
            VendorId = dto.VendorId,
            PropertyCode = dto.PropertyCode?.Trim(),
            EventType = PropertyUploadLogEvents.PropertyCreate,
            Operation = PropertyUploadLogOperations.CreateProperty
        };

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return await CompleteExternalPropertyAttemptAsync(BadRequest(errorMessage ?? "Invalid request data"), attempt, errorMessage);

        var accessError = await ValidateExternalPropertyAccessAsync(dto.OrganizationId, dto.OfficeId);
        if (accessError != null)
            return await CompleteExternalPropertyAttemptAsync(accessError, attempt);

        try
        {
            var propertyCode = dto.PropertyCode.Trim();
            var existingProperty = await _propertyRepository.GetPropertyByCodeAsync(propertyCode, dto.OrganizationId);
            if (existingProperty != null)
                return await CompleteExternalPropertyAttemptAsync(Conflict("Property Code already exists"), attempt, "Property Code already exists");

            var propertyDto = dto.ToCreatePropertyDto(propertyCode);
            var (propertyDtoIsValid, propertyDtoErrorMessage) = propertyDto.IsValid();
            if (!propertyDtoIsValid)
                return await CompleteExternalPropertyAttemptAsync(BadRequest(propertyDtoErrorMessage ?? "Invalid request data"), attempt, propertyDtoErrorMessage);

            var createdProperty = await _propertyRepository.CreateAsync(propertyDto.ToModel(ExternalPropertySystemUserId));
            return await CompleteExternalPropertyAttemptAsync(
                Ok(new PropertyResponseDto(createdProperty)),
                attempt,
                $"Property {createdProperty.PropertyCode} created.",
                createdProperty.PropertyId);
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
            return await CompleteExternalPropertyAttemptAsync(ServerError("An error occurred while saving the property"), attempt, ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPut("external")]
    public async Task<IActionResult> UpdateExternalProperty([FromBody] JsonElement body)
    {
        ExternalPropertyApiAttemptLog attempt = new ExternalPropertyApiAttemptLog
        {
            EventType = PropertyUploadLogEvents.PropertyUpdate,
            Operation = PropertyUploadLogOperations.UpdateProperty
        };
        if (ExternalPropertyPatchMerger.TryGetOrganizationContextForLogging(body, out var organizationId, out var officeId, out var vendorId, out var propertyCode))
        {
            attempt = new ExternalPropertyApiAttemptLog
            {
                OrganizationId = organizationId,
                OfficeId = officeId,
                VendorId = vendorId,
                PropertyCode = propertyCode,
                EventType = PropertyUploadLogEvents.PropertyUpdate,
                Operation = PropertyUploadLogOperations.UpdateProperty
            };
        }

        var (keysParsed, keys, keysError) = ExternalPropertyPatchMerger.TryParseRequiredKeys(body);
        if (!keysParsed || keys == null)
            return await CompleteExternalPropertyAttemptAsync(BadRequest(keysError ?? "Invalid request data"), attempt, keysError);

        attempt = new ExternalPropertyApiAttemptLog
        {
            OrganizationId = keys.OrganizationId,
            OfficeId = keys.OfficeId,
            VendorId = keys.VendorId,
            PropertyCode = keys.PropertyCode,
            EventType = PropertyUploadLogEvents.PropertyUpdate,
            Operation = PropertyUploadLogOperations.UpdateProperty
        };

        var accessError = await ValidateExternalPropertyAccessAsync(keys.OrganizationId, keys.OfficeId);
        if (accessError != null)
            return await CompleteExternalPropertyAttemptAsync(accessError, attempt);

        try
        {
            var existingProperty = await _propertyRepository.GetPropertyByCodeAsync(keys.PropertyCode, keys.OrganizationId);
            if (existingProperty == null)
                return await CompleteExternalPropertyAttemptAsync(NotFound("Property not found"), attempt, "Property not found");

            var (merged, updateDto, mergeError) = ExternalPropertyPatchMerger.TryMerge(existingProperty, body, keys);
            if (!merged || updateDto == null)
                return await CompleteExternalPropertyAttemptAsync(BadRequest(mergeError ?? "Invalid request data"), attempt, mergeError);

            var (updateResult, updateError) = await TryUpdateExternalPropertyAsync(existingProperty, updateDto);
            if (updateResult == null)
                return await CompleteExternalPropertyAttemptAsync(BadRequest(updateError ?? "Invalid request data"), attempt, updateError);

            return await CompleteExternalPropertyAttemptAsync(
                Ok(new PropertyResponseDto(updateResult)),
                attempt,
                $"Property {updateResult.PropertyCode} updated.",
                updateResult.PropertyId);
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
            return await CompleteExternalPropertyAttemptAsync(ServerError("An error occurred while saving the property"), attempt, ex.Message);
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

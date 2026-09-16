using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> CreateExternalProperty([FromBody] CreateExternalPropertyRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Property data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var organizationAccessError = await ValidateExternalPropertyRequestOrganizationAsync(dto.Properties);
        if (organizationAccessError != null)
            return organizationAccessError;

        return Ok(await ProcessExternalPropertyUpsertsAsync(dto.Properties, PropertyUploadLogOperations.CreateProperty));
    }

    [AllowAnonymous]
    [HttpPut("external")]
    public async Task<IActionResult> UpdateExternalProperty([FromBody] JsonElement body)
    {
        var (propertiesParsed, properties, parseError) = TryParseExternalPropertyRequestArray(body);
        if (!propertiesParsed || properties == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organizationAccessError = await ValidateExternalPropertyRequestOrganizationAsync(properties);
        if (organizationAccessError != null)
            return organizationAccessError;

        var response = new ExternalPropertyBatchResponseDto();
        for (var index = 0; index < properties.Count; index++)
        {
            var propertyBody = properties[index];
            var itemResult = new ExternalPropertyBatchItemResultDto { Index = index };

            var patchResult = await PatchExternalPropertyAsync(propertyBody, PropertyUploadLogOperations.UpdateProperty);
            await CompleteExternalPropertyAttemptAsync(
                patchResult.Success ? Ok(patchResult.Property) : BadRequest(patchResult.ErrorMessage),
                patchResult.Attempt,
                patchResult.Detail ?? patchResult.ErrorMessage,
                patchResult.Property?.PropertyId);

            itemResult.PropertyCode = patchResult.PropertyCode;
            itemResult.Success = patchResult.Success;
            itemResult.Updated = patchResult.Success;
            itemResult.ErrorMessage = patchResult.ErrorMessage;
            itemResult.Property = patchResult.Property;
            response.Results.Add(itemResult);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        return Ok(response);
    }

    private async Task<ExternalPropertyBatchResponseDto> ProcessExternalPropertyUpsertsAsync(
        IReadOnlyList<CreateExternalPropertyDto> properties,
        string operation)
    {
        var response = new ExternalPropertyBatchResponseDto();
        for (var index = 0; index < properties.Count; index++)
        {
            var propertyDto = properties[index];
            var itemResult = new ExternalPropertyBatchItemResultDto
            {
                Index = index,
                PropertyCode = (propertyDto.PropertyCode ?? string.Empty).Trim()
            };

            var accessError = await ValidateExternalPropertyAccessAsync(propertyDto.OrganizationId, propertyDto.OfficeId);
            if (accessError != null)
            {
                itemResult.ErrorMessage = ExtractExternalPropertyAttemptDetail(accessError) ?? "Invalid request data";
                response.Results.Add(itemResult);
                continue;
            }

            var upsertResult = await UpsertExternalPropertyAsync(propertyDto, operation);
            await CompleteExternalPropertyAttemptAsync(
                upsertResult.Success ? Ok(upsertResult.Property) : BadRequest(upsertResult.ErrorMessage),
                upsertResult.Attempt,
                upsertResult.Detail ?? upsertResult.ErrorMessage,
                upsertResult.Property?.PropertyId);

            itemResult.Success = upsertResult.Success;
            itemResult.Updated = upsertResult.Updated;
            itemResult.ErrorMessage = upsertResult.ErrorMessage;
            itemResult.Property = upsertResult.Property;
            response.Results.Add(itemResult);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        return response;
    }

    private async Task<IActionResult?> ValidateExternalPropertyRequestOrganizationAsync(IReadOnlyList<CreateExternalPropertyDto> properties)
    {
        if (properties.Count == 0)
            return BadRequest("Properties must contain at least one item");

        return await ValidateExternalPropertyOrganizationAccessAsync(properties[0].OrganizationId);
    }

    private async Task<IActionResult?> ValidateExternalPropertyRequestOrganizationAsync(IReadOnlyList<JsonElement> properties)
    {
        if (properties.Count == 0)
            return BadRequest("Properties must contain at least one item");

        var organizationIds = new HashSet<Guid>();
        foreach (var property in properties)
        {
            if (ExternalPropertyPatchMerger.TryGetOrganizationContextForLogging(property, out var organizationId, out _, out _, out _)
                && organizationId != Guid.Empty)
            {
                organizationIds.Add(organizationId);
            }
        }

        if (organizationIds.Count == 0)
            return BadRequest("OrganizationId is required");

        if (organizationIds.Count > 1)
            return BadRequest("All properties must use the same OrganizationId");

        return await ValidateExternalPropertyOrganizationAccessAsync(organizationIds.First());
    }

    private static (bool Success, List<JsonElement>? Properties, string? ErrorMessage) TryParseExternalPropertyRequestArray(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Property data is required");

        if (!body.TryGetProperty("properties", out var propertiesElement) || propertiesElement.ValueKind != JsonValueKind.Array)
            return (false, null, "Properties must contain at least one item");

        var properties = propertiesElement.EnumerateArray().ToList();
        if (properties.Count == 0)
            return (false, null, "Properties must contain at least one item");

        if (properties.Count > CreateExternalPropertyRequestDto.MaxPropertiesPerRequest)
            return (false, null, $"Properties cannot exceed {CreateExternalPropertyRequestDto.MaxPropertiesPerRequest} items per request");

        return (true, properties, null);
    }

    private sealed record ExternalPropertyPatchResult(
        bool Success,
        string PropertyCode,
        PropertyResponseDto? Property,
        string? ErrorMessage,
        string? Detail,
        ExternalPropertyApiAttemptLog Attempt);

    private async Task<ExternalPropertyPatchResult> PatchExternalPropertyAsync(JsonElement body, string operation)
    {
        ExternalPropertyApiAttemptLog attempt = new ExternalPropertyApiAttemptLog
        {
            EventType = PropertyUploadLogEvents.PropertyUpdate,
            Operation = operation
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
                Operation = operation
            };
        }

        var (keysParsed, keys, keysError) = ExternalPropertyPatchMerger.TryParseRequiredKeys(body);
        if (!keysParsed || keys == null)
        {
            return new ExternalPropertyPatchResult(
                false,
                propertyCode,
                null,
                keysError ?? "Invalid request data",
                keysError,
                attempt);
        }

        attempt = new ExternalPropertyApiAttemptLog
        {
            OrganizationId = keys.OrganizationId,
            OfficeId = keys.OfficeId,
            VendorId = keys.VendorId,
            PropertyCode = keys.PropertyCode,
            EventType = PropertyUploadLogEvents.PropertyUpdate,
            Operation = operation
        };

        var accessError = await ValidateExternalPropertyAccessAsync(keys.OrganizationId, keys.OfficeId);
        if (accessError != null)
        {
            return new ExternalPropertyPatchResult(
                false,
                keys.PropertyCode,
                null,
                ExtractExternalPropertyAttemptDetail(accessError) ?? "Invalid request data",
                ExtractExternalPropertyAttemptDetail(accessError),
                attempt);
        }

        try
        {
            var (existingProperty, isExactMatch, resolveError) = await ResolveExternalPropertyByKeysAsync(keys);
            if (resolveError != null)
            {
                return new ExternalPropertyPatchResult(
                    false,
                    keys.PropertyCode,
                    null,
                    "Property not found",
                    "Property not found",
                    attempt);
            }

            if (!isExactMatch || existingProperty == null)
            {
                return new ExternalPropertyPatchResult(
                    false,
                    keys.PropertyCode,
                    null,
                    "Property not found",
                    "Property not found",
                    attempt);
            }

            var (merged, updateDto, mergeError) = ExternalPropertyPatchMerger.TryMerge(existingProperty, body, keys);
            if (!merged || updateDto == null)
            {
                return new ExternalPropertyPatchResult(
                    false,
                    keys.PropertyCode,
                    null,
                    mergeError ?? "Invalid request data",
                    mergeError,
                    attempt);
            }

            var (updateResult, updateError) = await TryUpdateExternalPropertyAsync(existingProperty, updateDto);
            if (updateResult == null)
            {
                return new ExternalPropertyPatchResult(
                    false,
                    keys.PropertyCode,
                    null,
                    updateError ?? "Invalid request data",
                    updateError,
                    attempt);
            }

            var updatedProperty = new PropertyResponseDto(updateResult);
            return new ExternalPropertyPatchResult(
                true,
                updatedProperty.PropertyCode,
                updatedProperty,
                null,
                $"Property {updatedProperty.PropertyCode} updated.",
                attempt);
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
            return new ExternalPropertyPatchResult(
                false,
                keys.PropertyCode,
                null,
                "An error occurred while saving the property",
                ex.Message,
                attempt);
        }
    }

    private async Task<(Property? Property, bool IsExactMatch, IActionResult? ErrorResult)> ResolveExternalPropertyByKeysAsync(ExternalPropertyKeyDto keys)
    {
        var property = await _propertyRepository.GetPropertyByCodeAsync(keys.PropertyCode, keys.OrganizationId);
        if (property == null)
            return (null, false, null);

        if (property.OrganizationId != keys.OrganizationId
            || property.OfficeId != keys.OfficeId
            || property.VendorId != keys.VendorId)
        {
            return (null, false, NotFound("Property not found"));
        }

        return (property, true, null);
    }

    private async Task<IActionResult?> ValidateExternalPropertyOrganizationAccessAsync(Guid organizationId)
    {
        var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalPropertyKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        SetApplicationLogContext(organizationId, null);
        return null;
    }

    private async Task<IActionResult?> ValidateExternalPropertyAccessAsync(Guid organizationId, int officeId)
    {
        var organizationError = await ValidateExternalPropertyOrganizationAccessAsync(organizationId);
        if (organizationError != null)
            return organizationError;

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        if (office == null)
            return BadRequest("Invalid OfficeId for OrganizationId");

        SetApplicationLogContext(organizationId, officeId);
        return null;
    }

    private static ExternalPropertyApiAttemptLog BuildExternalPropertyAttempt(
        CreateExternalPropertyDto dto,
        string eventType,
        string operation)
    {
        return new ExternalPropertyApiAttemptLog
        {
            OrganizationId = dto.OrganizationId,
            OfficeId = dto.OfficeId,
            VendorId = dto.VendorId,
            PropertyCode = dto.PropertyCode?.Trim(),
            EventType = eventType,
            Operation = operation
        };
    }

    private sealed record ExternalPropertyUpsertResult(
        bool Success,
        bool Updated,
        int StatusCode,
        PropertyResponseDto? Property,
        string? ErrorMessage,
        string? Detail,
        ExternalPropertyApiAttemptLog Attempt);

    private async Task<ExternalPropertyUpsertResult> UpsertExternalPropertyAsync(
        CreateExternalPropertyDto dto,
        string operation)
    {
        var propertyCode = (dto.PropertyCode ?? string.Empty).Trim();
        var attempt = BuildExternalPropertyAttempt(dto, PropertyUploadLogEvents.PropertyCreate, operation);
        var keys = new ExternalPropertyKeyDto
        {
            OrganizationId = dto.OrganizationId,
            OfficeId = dto.OfficeId,
            VendorId = dto.VendorId,
            PropertyCode = propertyCode
        };

        try
        {
            var (existingProperty, isExactMatch, resolveError) = await ResolveExternalPropertyByKeysAsync(keys);
            if (resolveError != null)
            {
                return new ExternalPropertyUpsertResult(
                    false,
                    false,
                    StatusCodes.Status404NotFound,
                    null,
                    "Property not found",
                    "Property not found",
                    attempt);
            }

            if (isExactMatch && existingProperty != null)
            {
                attempt = BuildExternalPropertyAttempt(dto, PropertyUploadLogEvents.PropertyUpdate, operation);
                var updateDto = dto.ToUpdatePropertyDto(existingProperty, propertyCode);
                var (updateResult, updateError) = await TryUpdateExternalPropertyAsync(existingProperty, updateDto);
                if (updateResult == null)
                {
                    return new ExternalPropertyUpsertResult(
                        false,
                        true,
                        StatusCodes.Status400BadRequest,
                        null,
                        updateError ?? "Invalid request data",
                        updateError,
                        attempt);
                }

                var updatedProperty = new PropertyResponseDto(updateResult);
                return new ExternalPropertyUpsertResult(
                    true,
                    true,
                    StatusCodes.Status200OK,
                    updatedProperty,
                    null,
                    $"Property {updatedProperty.PropertyCode} updated.",
                    attempt);
            }

            var createDto = dto.ToCreatePropertyDto(propertyCode);
            var (createIsValid, createErrorMessage) = createDto.IsValid();
            if (!createIsValid)
            {
                return new ExternalPropertyUpsertResult(
                    false,
                    false,
                    StatusCodes.Status400BadRequest,
                    null,
                    createErrorMessage ?? "Invalid request data",
                    createErrorMessage,
                    attempt);
            }

            var createdProperty = await _propertyRepository.CreateAsync(createDto.ToModel(SystemUserId));
            var createdResponse = new PropertyResponseDto(createdProperty);
            return new ExternalPropertyUpsertResult(
                true,
                false,
                StatusCodes.Status200OK,
                createdResponse,
                null,
                $"Property {createdResponse.PropertyCode} created.",
                attempt);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error upserting external property intake request. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}",
                dto.OrganizationId,
                dto.OfficeId,
                dto.PropertyCode,
                dto.VendorId);
            return new ExternalPropertyUpsertResult(
                false,
                false,
                StatusCodes.Status500InternalServerError,
                null,
                "An error occurred while saving the property",
                ex.Message,
                attempt);
        }
    }

    private async Task<(Property? Property, string? ErrorMessage)> TryUpdateExternalPropertyAsync(Property existingProperty, UpdatePropertyDto updateDto)
    {
        var (updateIsValid, updateErrorMessage) = updateDto.IsValid();
        if (!updateIsValid)
            return (null, updateErrorMessage ?? "Invalid request data");

        var property = updateDto.ToModel(SystemUserId);
        if (existingProperty.OfficeId != updateDto.OfficeId)
            await _propertyManager.UpdatePropertyOfficeAsync(property, SystemUserId);

        var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
        return (updatedProperty, null);
    }
}

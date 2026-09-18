using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Api.Services;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> CreateExternalProperty([FromBody] JsonElement body)
    {
        var (parsed, context, properties, parseError) = CreateExternalPropertyRequestDto.TryParseFromBody(body);
        if (!parsed || context == null || properties == null)
            return await RejectExternalPropertyRequestAsync(body, PropertyUploadLogOperations.CreateProperty, PropertyUploadLogEvents.PropertyCreate, parseError ?? "Invalid request data");

        var organizationAccessError = await ValidateExternalPropertyOrganizationAccessAsync(context.OrganizationId);
        if (organizationAccessError != null)
            return organizationAccessError;

        var accessError = await ValidateExternalPropertyAccessAsync(context.OrganizationId, context.OfficeId);
        if (accessError != null)
            return accessError;

        var partnerError = await ValidateExternalPropertyPartnerAsync(context.OrganizationId, context.PartnerVendorId);
        if (partnerError != null)
            return partnerError;

        return Ok(await ProcessExternalPropertyUpsertsAsync(context, properties, PropertyUploadLogOperations.CreateProperty));
    }

    [AllowAnonymous]
    [HttpPut("external")]
    public async Task<IActionResult> UpdateExternalProperty([FromBody] JsonElement body)
    {
        var (contextParsed, context, contextError) = ExternalPropertyOwnerContactResolver.TryParseIntakeContext(body);
        if (!contextParsed || context == null)
            return await RejectExternalPropertyRequestAsync(body, PropertyUploadLogOperations.UpdateProperty, PropertyUploadLogEvents.PropertyUpdate, contextError ?? "Invalid request data");

        var (propertiesParsed, properties, parseError) = TryParseExternalPropertyRequestArray(body);
        if (!propertiesParsed || properties == null)
            return await RejectExternalPropertyRequestAsync(body, PropertyUploadLogOperations.UpdateProperty, PropertyUploadLogEvents.PropertyUpdate, parseError ?? "Invalid request data", context);

        var organizationAccessError = await ValidateExternalPropertyOrganizationAccessAsync(context.OrganizationId);
        if (organizationAccessError != null)
            return organizationAccessError;

        var accessError = await ValidateExternalPropertyAccessAsync(context.OrganizationId, context.OfficeId);
        if (accessError != null)
            return accessError;

        var partnerError = await ValidateExternalPropertyPartnerAsync(context.OrganizationId, context.PartnerVendorId);
        if (partnerError != null)
            return partnerError;

        var response = new ExternalPropertyBatchResponseDto();
        for (var index = 0; index < properties.Count; index++)
        {
            var propertyBody = properties[index];
            var itemResult = new ExternalPropertyBatchItemResultDto { Index = index };

            var patchResult = await PatchExternalPropertyAsync(propertyBody, context, PropertyUploadLogOperations.UpdateProperty, index);
            await CompleteExternalPropertyAttemptAsync(
                patchResult.Success ? Ok(patchResult.Property) : BadRequest(patchResult.ErrorMessage ?? "Invalid request data"),
                patchResult.Attempt,
                patchResult.Detail ?? patchResult.ErrorMessage,
                patchResult.Property?.PropertyId);

            itemResult.PropertyCode = patchResult.PropertyCode;
            itemResult.Success = patchResult.Success;
            itemResult.Updated = patchResult.Success;
            itemResult.ErrorMessage = patchResult.ErrorMessage;
            itemResult.Property = patchResult.Property;
            itemResult.PhotoImport = patchResult.PhotoImport;
            itemResult.PhotoImportError = patchResult.PhotoImportError;

            if (patchResult.PhotoImport != null)
            {
                await CompleteExternalPropertyAttemptAsync(
                    Accepted(patchResult.PhotoImport),
                    new ExternalPropertyApiAttemptLog
                    {
                        OrganizationId = context.OrganizationId,
                        OfficeId = context.OfficeId,
                        VendorId = context.PartnerVendorId,
                        PropertyId = patchResult.Property?.PropertyId,
                        PropertyCode = patchResult.PropertyCode,
                        ImportId = patchResult.PhotoImport.ImportId,
                        EventType = PropertyUploadLogEvents.PhotoImportQueue,
                        Operation = PropertyUploadLogOperations.QueuePhotoImport
                    },
                    $"Queued {patchResult.PhotoImport.PhotoCount} photo URL(s). ImportId={patchResult.PhotoImport.ImportId}.",
                    patchResult.Property?.PropertyId);
            }

            response.Results.Add(itemResult);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        return Ok(response);
    }

    private async Task<ExternalPropertyBatchResponseDto> ProcessExternalPropertyUpsertsAsync(
        ExternalPropertyIntakeContext context,
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

            var upsertResult = await UpsertExternalPropertyAsync(propertyDto, context, operation);
            await CompleteExternalPropertyAttemptAsync(
                upsertResult.Success ? Ok(upsertResult.Property) : BadRequest(upsertResult.ErrorMessage ?? "Invalid request data"),
                upsertResult.Attempt,
                upsertResult.Detail ?? upsertResult.ErrorMessage,
                upsertResult.Property?.PropertyId);

            itemResult.Success = upsertResult.Success;
            itemResult.Updated = upsertResult.Updated;
            itemResult.ErrorMessage = upsertResult.ErrorMessage;
            itemResult.Property = upsertResult.Property;
            itemResult.PhotoImport = upsertResult.PhotoImport;
            itemResult.PhotoImportError = upsertResult.PhotoImportError;

            if (upsertResult.PhotoImport != null)
            {
                await CompleteExternalPropertyAttemptAsync(
                    Accepted(upsertResult.PhotoImport),
                    new ExternalPropertyApiAttemptLog
                    {
                        OrganizationId = context.OrganizationId,
                        OfficeId = context.OfficeId,
                        VendorId = context.PartnerVendorId,
                        PropertyId = upsertResult.Property?.PropertyId,
                        PropertyCode = itemResult.PropertyCode,
                        ImportId = upsertResult.PhotoImport.ImportId,
                        EventType = PropertyUploadLogEvents.PhotoImportQueue,
                        Operation = PropertyUploadLogOperations.QueuePhotoImport
                    },
                    $"Queued {upsertResult.PhotoImport.PhotoCount} photo URL(s). ImportId={upsertResult.PhotoImport.ImportId}.",
                    upsertResult.Property?.PropertyId);
            }

            response.Results.Add(itemResult);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        return response;
    }

    private static (bool Success, List<JsonElement>? Properties, string? ErrorMessage) TryParseExternalPropertyRequestArray(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Property data is required");

        var (propertiesParsed, propertiesElement, propertiesError) = ExternalPropertyIntakeJson.TryParseRequiredPropertiesArray(body);
        if (!propertiesParsed)
            return (false, null, propertiesError ?? "Properties must contain at least one item");

        var properties = propertiesElement.EnumerateArray().ToList();

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
        ExternalPropertyApiAttemptLog Attempt,
        ExternalPropertyPhotoImportCreatedResponseDto? PhotoImport = null,
        string? PhotoImportError = null);

    private async Task<ExternalPropertyPatchResult> PatchExternalPropertyAsync(JsonElement body, ExternalPropertyIntakeContext context, string operation, int index)
    {
        var prefix = $"Properties[{index}]";
        var propertyBody = body.ValueKind == JsonValueKind.Object
            ? ExternalPropertyIntakeJson.StripRequestHeaderFields(body)
            : body;
        var jsonErrors = ExternalPropertyIntakeErrors.CollectFromJson(propertyBody, prefix, requireCreateFields: false);
        var (keysParsed, keys, keysError) = ExternalPropertyOwnerContactResolver.TryParsePropertyKeys(propertyBody, context);
        ExternalPropertyApiAttemptLog attempt = new ExternalPropertyApiAttemptLog
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            VendorId = context.PartnerVendorId,
            PropertyCode = keys?.PropertyCode,
            EventType = PropertyUploadLogEvents.PropertyUpdate,
            Operation = operation
        };

        if (!keysParsed || keys == null)
        {
            if (!string.IsNullOrWhiteSpace(keysError))
                jsonErrors.Insert(0, $"{prefix}: {keysError}");
            var keyError = jsonErrors.Count > 0 ? ExternalPropertyIntakeErrors.Join(jsonErrors) : (keysError ?? "Invalid request data");
            return new ExternalPropertyPatchResult(false, keys?.PropertyCode ?? string.Empty, null, keyError, keyError, attempt);
        }

        if (jsonErrors.Count > 0)
        {
            var jsonError = ExternalPropertyIntakeErrors.Join(jsonErrors);
            return new ExternalPropertyPatchResult(false, keys.PropertyCode, null, jsonError, jsonError, attempt);
        }

        var photosFieldPresent = ExternalPropertyPhotoSyncService.TryParsePhotosField(propertyBody, out var patchPhotos, out var photosParseError);
        if (photosParseError != null)
        {
            return new ExternalPropertyPatchResult(
                false,
                keys.PropertyCode,
                null,
                photosParseError,
                photosParseError,
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
                    ExternalPropertyIntakeErrors.Prefix(prefix, "Property not found"),
                    "Property not found",
                    attempt);
            }

            if (!isExactMatch || existingProperty == null)
            {
                return new ExternalPropertyPatchResult(
                    false,
                    keys.PropertyCode,
                    null,
                    ExternalPropertyIntakeErrors.Prefix(prefix, "Property not found"),
                    "Property not found",
                    attempt);
            }

            var (merged, updateDto, mergeError) = ExternalPropertyPatchMerger.TryMerge(existingProperty, propertyBody, keys);
            if (!merged || updateDto == null)
            {
                var prefixedMerge = ExternalPropertyIntakeErrors.Prefix(prefix, mergeError);
                return new ExternalPropertyPatchResult(false, keys.PropertyCode, null, prefixedMerge, prefixedMerge, attempt);
            }

            var (contactsApplied, contactPatchError) = await _externalPropertyOwnerContactResolver.TryApplyContactPatchesAsync(propertyBody, context, updateDto, SystemUserId);
            if (!contactsApplied)
            {
                var prefixedContact = ExternalPropertyIntakeErrors.Prefix(prefix, contactPatchError);
                return new ExternalPropertyPatchResult(false, keys.PropertyCode, null, prefixedContact, prefixedContact, attempt);
            }

            var vendorContactInRequest = ExternalPropertyOwnerContactResolver.TryGetContact(propertyBody, "vendor", out _);
            var (leaseContactsValid, leaseContactsError) = CreateExternalPropertyDto.ValidateLeaseTypeContacts(
                updateDto.PropertyLeaseTypeId,
                updateDto.Owner1Id,
                updateDto.VendorId,
                vendorContactInRequest);
            if (!leaseContactsValid)
            {
                var prefixedLease = ExternalPropertyIntakeErrors.Prefix(prefix, leaseContactsError);
                return new ExternalPropertyPatchResult(false, keys.PropertyCode, null, prefixedLease, prefixedLease, attempt);
            }

            if ((PropertyLeaseType)updateDto.PropertyLeaseTypeId == PropertyLeaseType.PropertyManagement)
                updateDto.VendorId = null;

            var (updateResult, updateError) = await TryUpdateExternalPropertyAsync(existingProperty, updateDto);
            if (updateResult == null)
            {
                var prefixedUpdate = ExternalPropertyIntakeErrors.Prefix(prefix, updateError);
                return new ExternalPropertyPatchResult(false, keys.PropertyCode, null, prefixedUpdate, prefixedUpdate, attempt);
            }

            var updatedProperty = new PropertyResponseDto(updateResult);
            var detail = $"Property {updatedProperty.PropertyCode} updated.";
            ExternalPropertyPhotoImportCreatedResponseDto? photoImport = null;
            string? photoImportError = null;

            if (photosFieldPresent && patchPhotos != null)
            {
                var (syncImport, syncError, syncDetail) = await _externalPropertyPhotoSyncService.SyncPhotosAsync(keys, updateResult, patchPhotos);
                photoImport = syncImport;
                photoImportError = syncError;
                if (!string.IsNullOrWhiteSpace(syncDetail))
                    detail += $" {syncDetail}";

                if (syncError != null)
                {
                    return new ExternalPropertyPatchResult(
                        true,
                        updatedProperty.PropertyCode,
                        updatedProperty,
                        null,
                        detail,
                        attempt,
                        photoImport,
                        photoImportError);
                }
            }

            return new ExternalPropertyPatchResult(
                true,
                updatedProperty.PropertyCode,
                updatedProperty,
                null,
                detail,
                attempt,
                photoImport,
                photoImportError);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating external property intake request. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}",
                context.OrganizationId,
                context.OfficeId,
                keys.PropertyCode,
                context.PartnerVendorId);
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
        var property = await _propertyRepository.GetPropertyByCodeIncludingDeletedAsync(
            keys.PropertyCode,
            keys.OrganizationId,
            keys.OfficeId);
        if (property == null)
            return (null, false, null);

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

    private async Task<IActionResult?> ValidateExternalPropertyPartnerAsync(Guid organizationId, Guid partnerVendorId)
    {
        if (partnerVendorId == Guid.Empty)
            return BadRequest("VendorId is required");

        var partner = await _contactRepository.GetContactByIdsAsync(partnerVendorId, organizationId);
        if (partner == null || partner.EntityType != EntityType.Vendor)
            return BadRequest("Invalid VendorId");

        return null;
    }

    private static ExternalPropertyApiAttemptLog BuildExternalPropertyAttempt(
        CreateExternalPropertyDto dto,
        ExternalPropertyIntakeContext context,
        string eventType,
        string operation)
    {
        return new ExternalPropertyApiAttemptLog
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            VendorId = context.PartnerVendorId,
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
        ExternalPropertyApiAttemptLog Attempt,
        ExternalPropertyPhotoImportCreatedResponseDto? PhotoImport = null,
        string? PhotoImportError = null);

    private async Task<ExternalPropertyUpsertResult> UpsertExternalPropertyAsync(
        CreateExternalPropertyDto dto,
        ExternalPropertyIntakeContext context,
        string operation)
    {
        var propertyCode = (dto.PropertyCode ?? string.Empty).Trim();
        var attempt = BuildExternalPropertyAttempt(dto, context, PropertyUploadLogEvents.PropertyCreate, operation);
        var keys = new ExternalPropertyKeyDto
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            VendorId = context.PartnerVendorId,
            PropertyCode = propertyCode
        };

        try
        {
            var (contactsResolved, owner1Id, owner2Id, owner3Id, propertyVendorContactId, contactError) = await _externalPropertyOwnerContactResolver.ResolveContactsAsync(dto, context, SystemUserId);
            if (!contactsResolved)
            {
                return new ExternalPropertyUpsertResult(
                    false,
                    false,
                    StatusCodes.Status400BadRequest,
                    null,
                    contactError ?? "Invalid request data",
                    contactError,
                    attempt);
            }

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
                attempt = BuildExternalPropertyAttempt(dto, context, PropertyUploadLogEvents.PropertyUpdate, operation);
                var updateDto = dto.ToUpdatePropertyDto(existingProperty, propertyCode, context, owner1Id, owner2Id, owner3Id, propertyVendorContactId);
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
                var (updatedPhotoImport, updatedPhotoImportError, updatedPhotoSyncDetail) = await SyncPropertyPhotosIfProvidedAsync(dto, keys, updateResult);
                var updatedDetail = BuildUpsertDetail(updatedProperty.PropertyCode, true, updatedPhotoSyncDetail, updatedPhotoImportError);
                return new ExternalPropertyUpsertResult(
                    true,
                    true,
                    StatusCodes.Status200OK,
                    updatedProperty,
                    null,
                    updatedDetail,
                    attempt,
                    updatedPhotoImport,
                    updatedPhotoImportError);
            }

            var createDto = dto.ToCreatePropertyDto(propertyCode, context, owner1Id, owner2Id, owner3Id, propertyVendorContactId);
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
            var (createdPhotoImport, createdPhotoImportError, createdPhotoSyncDetail) = await SyncPropertyPhotosIfProvidedAsync(dto, keys, createdProperty);
            var createdDetail = BuildUpsertDetail(createdResponse.PropertyCode, false, createdPhotoSyncDetail, createdPhotoImportError);
            return new ExternalPropertyUpsertResult(
                true,
                false,
                StatusCodes.Status200OK,
                createdResponse,
                null,
                createdDetail,
                attempt,
                createdPhotoImport,
                createdPhotoImportError);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error upserting external property intake request. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}",
                context.OrganizationId,
                context.OfficeId,
                dto.PropertyCode,
                context.PartnerVendorId);

            var errorMessage = ExternalPropertyIntakeErrors.TranslateSaveError(GetExternalPropertySaveErrorMessage(ex));
            var isClientError = errorMessage.Contains("stored procedure", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("Violation of UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("Cannot insert the value NULL", StringComparison.OrdinalIgnoreCase);

            return new ExternalPropertyUpsertResult(
                false,
                false,
                isClientError ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError,
                null,
                errorMessage,
                ex.Message,
                attempt);
        }
    }

    private static string GetExternalPropertySaveErrorMessage(Exception ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (string.IsNullOrWhiteSpace(message))
            return "An error occurred while saving the property";

        return message.Length > 500 ? message[..500] : message;
    }

    private async Task<(ExternalPropertyPhotoImportCreatedResponseDto? PhotoImport, string? PhotoImportError, string? PhotoSyncDetail)> SyncPropertyPhotosIfProvidedAsync(
        CreateExternalPropertyDto dto,
        ExternalPropertyKeyDto keys,
        Property property)
    {
        if (dto.Photos == null)
            return (null, null, null);

        var (photoImport, syncError, syncDetail) = await _externalPropertyPhotoSyncService.SyncPhotosAsync(keys, property, dto.Photos);
        return (photoImport, syncError, syncDetail);
    }

    private static string BuildUpsertDetail(
        string propertyCode,
        bool updated,
        string? photoSyncDetail,
        string? photoImportError)
    {
        var detail = updated
            ? $"Property {propertyCode} updated."
            : $"Property {propertyCode} created.";

        if (!string.IsNullOrWhiteSpace(photoSyncDetail))
            detail += $" {photoSyncDetail}";

        if (!string.IsNullOrWhiteSpace(photoImportError))
            detail += $" Photo sync failed: {photoImportError}";

        return detail;
    }

    private async Task<(Property? Property, string? ErrorMessage)> TryUpdateExternalPropertyAsync(Property existingProperty, UpdatePropertyDto updateDto)
    {
        var (updateIsValid, updateErrorMessage) = updateDto.IsValid();
        if (!updateIsValid)
            return (null, updateErrorMessage ?? "Invalid request data");

        updateDto.IsDeleted = false;

        var property = updateDto.ToModel(SystemUserId);
        if (existingProperty.OfficeId != updateDto.OfficeId)
            await _propertyManager.UpdatePropertyOfficeAsync(property, SystemUserId);

        var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
        return (updatedProperty, null);
    }
}

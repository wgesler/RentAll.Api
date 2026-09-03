using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Api.Services;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    [AllowAnonymous]
    [HttpPost("external/{propertyCode}/photos")]
    public async Task<IActionResult> AddExternalPropertyPhotosAsync(string propertyCode, [FromBody] CreateExternalPropertyPhotosBatchDto dto)
    {
        if (dto == null)
            return BadRequest("Photo data is required");

        var attempt = new ExternalPropertyApiAttemptLog
        {
            OrganizationId = dto.OrganizationId,
            OfficeId = dto.OfficeId,
            VendorId = dto.VendorId,
            PropertyCode = dto.PropertyCode?.Trim(),
            EventType = PropertyUploadLogEvents.PhotoImportQueue,
            Operation = PropertyUploadLogOperations.QueuePhotoImport
        };

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return await CompleteExternalPropertyAttemptAsync(BadRequest(errorMessage ?? "Invalid request data"), attempt, errorMessage);

        var routeError = ValidateExternalPropertyCodeRoute(propertyCode, dto.PropertyCode);
        if (routeError != null)
            return await CompleteExternalPropertyAttemptAsync(BadRequest(routeError), attempt, routeError);

        var accessError = await ValidateExternalPropertyAccessAsync(dto.OrganizationId, dto.OfficeId);
        if (accessError != null)
            return await CompleteExternalPropertyAttemptAsync(accessError, attempt);

        try
        {
            var keys = dto.ToKeyDto();
            var (property, propertyError) = await ResolveExternalPropertyForPhotoAsync(keys);
            if (property == null)
            {
                var notFoundResult = propertyError ?? NotFound("Property not found");
                return await CompleteExternalPropertyAttemptAsync(notFoundResult, attempt, "Property not found");
            }

            var importId = Guid.NewGuid();
            var import = new PropertyPhotoImport
            {
                ImportId = importId,
                OrganizationId = keys.OrganizationId,
                OfficeId = keys.OfficeId,
                VendorId = keys.VendorId,
                PropertyId = property.PropertyId,
                PropertyCode = keys.PropertyCode,
                Status = PropertyPhotoImportStatus.Pending
            };

            var items = dto.Photos
                .Select((photo, index) => new PropertyPhotoImportItem
                {
                    ImportId = importId,
                    ItemIndex = index,
                    Url = photo.Url.Trim(),
                    SortOrder = photo.SortOrder,
                    Status = PropertyPhotoImportItemStatus.Pending
                })
                .ToList();

            await _propertyRepository.CreatePropertyPhotoImportAsync(import, items);

            await _externalPropertyUploadLogService.LogPhotoImportQueuedAsync(
                keys.OrganizationId,
                keys.OfficeId,
                keys.VendorId,
                property.PropertyId,
                keys.PropertyCode,
                importId,
                items.Count);

            return await CompleteExternalPropertyAttemptAsync(
                Accepted(new ExternalPropertyPhotoImportCreatedResponseDto
                {
                    ImportId = importId,
                    PropertyCode = keys.PropertyCode,
                    PropertyId = property.PropertyId,
                    Status = PropertyPhotoImportStatus.Pending.ToString(),
                    PhotoCount = items.Count
                }),
                new ExternalPropertyApiAttemptLog
                {
                    OrganizationId = keys.OrganizationId,
                    OfficeId = keys.OfficeId,
                    VendorId = keys.VendorId,
                    PropertyId = property.PropertyId,
                    PropertyCode = keys.PropertyCode,
                    ImportId = importId,
                    EventType = PropertyUploadLogEvents.PhotoImportQueue,
                    Operation = PropertyUploadLogOperations.QueuePhotoImport
                },
                $"Queued {items.Count} photo URL(s). ImportId={importId}.",
                property.PropertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error queueing external property photo import. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}, PhotoCount={PhotoCount}",
                dto.OrganizationId,
                dto.OfficeId,
                dto.PropertyCode,
                dto.VendorId,
                dto.Photos.Count);
            return await CompleteExternalPropertyAttemptAsync(ServerError("An error occurred while queueing property photos"), attempt, ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpGet("external/{propertyCode}/photos/import/{importId:guid}")]
    public async Task<IActionResult> GetExternalPropertyPhotoImportStatusAsync(
        string propertyCode,
        Guid importId,
        [FromQuery] ExternalPropertyPhotoImportStatusQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var attempt = new ExternalPropertyApiAttemptLog
        {
            OrganizationId = query.OrganizationId,
            OfficeId = query.OfficeId,
            VendorId = query.VendorId,
            PropertyCode = query.PropertyCode?.Trim(),
            ImportId = importId,
            EventType = PropertyUploadLogEvents.PhotoImportStatus,
            Operation = PropertyUploadLogOperations.GetPhotoImportStatus
        };

        var keys = new ExternalPropertyKeyDto
        {
            OrganizationId = query.OrganizationId,
            OfficeId = query.OfficeId,
            VendorId = query.VendorId,
            PropertyCode = query.PropertyCode.Trim()
        };

        var (keysAreValid, keysError) = new ExternalPropertyKeyRequest
        {
            OrganizationId = keys.OrganizationId,
            OfficeId = keys.OfficeId,
            VendorId = keys.VendorId,
            PropertyCode = keys.PropertyCode
        }.ValidateRequiredKeys();

        if (!keysAreValid)
            return await CompleteExternalPropertyAttemptAsync(BadRequest(keysError ?? "Invalid request data"), attempt, keysError);

        var routeError = ValidateExternalPropertyCodeRoute(propertyCode, keys.PropertyCode);
        if (routeError != null)
            return await CompleteExternalPropertyAttemptAsync(BadRequest(routeError), attempt, routeError);

        var accessError = await ValidateExternalPropertyAccessAsync(keys.OrganizationId, keys.OfficeId);
        if (accessError != null)
            return await CompleteExternalPropertyAttemptAsync(accessError, attempt);

        try
        {
            var import = await _propertyRepository.GetPropertyPhotoImportByIdAsync(importId, keys.OrganizationId);
            if (import == null)
                return await CompleteExternalPropertyAttemptAsync(NotFound("Import not found"), attempt, "Import not found");

            if (!string.Equals(import.PropertyCode, keys.PropertyCode, StringComparison.OrdinalIgnoreCase)
                || import.OfficeId != keys.OfficeId
                || import.VendorId != keys.VendorId)
                return await CompleteExternalPropertyAttemptAsync(NotFound("Import not found"), attempt, "Import not found");

            SetApplicationLogContext(keys.OrganizationId, keys.OfficeId);

            var items = (await _propertyRepository.GetPropertyPhotoImportItemsByImportIdAsync(importId)).ToList();
            var response = new ExternalPropertyPhotoImportStatusResponseDto
            {
                ImportId = import.ImportId,
                PropertyCode = import.PropertyCode,
                PropertyId = import.PropertyId,
                Status = import.Status.ToString(),
                CreatedOn = import.CreatedOn,
                CompletedOn = import.CompletedOn,
                TotalCount = items.Count,
                CompletedCount = items.Count(x => x.Status == PropertyPhotoImportItemStatus.Completed),
                FailedCount = items.Count(x => x.Status == PropertyPhotoImportItemStatus.Failed),
                PendingCount = items.Count(x => x.Status is PropertyPhotoImportItemStatus.Pending or PropertyPhotoImportItemStatus.InProgress),
                Items = items.Select(item => new ExternalPropertyPhotoImportItemStatusDto
                {
                    Index = item.ItemIndex,
                    Url = item.Url,
                    SortOrder = item.SortOrder,
                    Status = item.Status.ToString(),
                    PhotoId = item.PhotoId,
                    ErrorMessage = item.ErrorMessage
                }).ToList()
            };

            return await CompleteExternalPropertyAttemptAsync(
                Ok(response),
                new ExternalPropertyApiAttemptLog
                {
                    OrganizationId = keys.OrganizationId,
                    OfficeId = keys.OfficeId,
                    VendorId = keys.VendorId,
                    PropertyId = import.PropertyId,
                    PropertyCode = keys.PropertyCode,
                    ImportId = importId,
                    EventType = PropertyUploadLogEvents.PhotoImportStatus,
                    Operation = PropertyUploadLogOperations.GetPhotoImportStatus
                },
                $"Import status {import.Status}; completed={response.CompletedCount}, failed={response.FailedCount}, pending={response.PendingCount}.",
                import.PropertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting external property photo import status. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}, ImportId={ImportId}",
                keys.OrganizationId,
                keys.OfficeId,
                keys.PropertyCode,
                keys.VendorId,
                importId);
            return await CompleteExternalPropertyAttemptAsync(ServerError("An error occurred while getting import status"), attempt, ex.Message);
        }
    }

    private static string? ValidateExternalPropertyCodeRoute(string routePropertyCode, string bodyPropertyCode)
    {
        var normalizedRouteCode = routePropertyCode?.Trim() ?? string.Empty;
        var normalizedBodyCode = bodyPropertyCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedRouteCode))
            return "PropertyCode is required";

        if (!string.Equals(normalizedRouteCode, normalizedBodyCode, StringComparison.OrdinalIgnoreCase))
            return "PropertyCode in URL must match PropertyCode in body";

        return null;
    }

    private async Task<(Property? Property, IActionResult? ErrorResult)> ResolveExternalPropertyForPhotoAsync(ExternalPropertyKeyDto keys)
    {
        var property = await _propertyRepository.GetPropertyByCodeAsync(keys.PropertyCode, keys.OrganizationId);
        if (property == null || property.OrganizationId != keys.OrganizationId)
            return (null, NotFound("Property not found"));

        if (property.OfficeId != keys.OfficeId || property.VendorId != keys.VendorId)
            return (null, NotFound("Property not found"));

        SetApplicationLogContext(keys.OrganizationId, keys.OfficeId);
        return (property, null);
    }
}

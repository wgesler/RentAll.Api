using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Api.Services;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
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
            PropertyCode = (query.PropertyCode ?? string.Empty).Trim()
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

    private static string? ValidateExternalPropertyCodeRoute(string routePropertyCode, string? bodyPropertyCode)
    {
        var normalizedRouteCode = routePropertyCode?.Trim() ?? string.Empty;
        var normalizedBodyCode = bodyPropertyCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedRouteCode))
            return "PropertyCode is required";

        if (!string.Equals(normalizedRouteCode, normalizedBodyCode, StringComparison.OrdinalIgnoreCase))
            return "PropertyCode in URL must match PropertyCode in body";

        return null;
    }
}

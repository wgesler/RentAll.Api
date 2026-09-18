using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Properties;
using System.Text.Json;

namespace RentAll.Api.Services;

public class ExternalPropertyPhotoSyncService
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IFileService _fileService;
    private readonly ExternalPropertyUploadLogService _uploadLogService;
    private readonly ILogger<ExternalPropertyPhotoSyncService> _logger;

    public ExternalPropertyPhotoSyncService(
        IPropertyRepository propertyRepository,
        IFileService fileService,
        ExternalPropertyUploadLogService uploadLogService,
        ILogger<ExternalPropertyPhotoSyncService> logger)
    {
        _propertyRepository = propertyRepository;
        _fileService = fileService;
        _uploadLogService = uploadLogService;
        _logger = logger;
    }

    public static bool TryParsePhotosField(JsonElement body, out List<ExternalPropertyPhotoUrlItemDto>? photos, out string? errorMessage)
    {
        photos = null;
        errorMessage = null;

        if (!TryGetProperty(body, "photos", out var photosElement))
            return false;

        if (photosElement.ValueKind != JsonValueKind.Array)
        {
            errorMessage = "Photos must be an array when provided";
            return true;
        }

        var parsedPhotos = new List<ExternalPropertyPhotoUrlItemDto>();
        var index = 0;
        foreach (var photoElement in photosElement.EnumerateArray())
        {
            if (photoElement.ValueKind != JsonValueKind.Object)
            {
                errorMessage = $"Photos[{index}] must be an object";
                photos = parsedPhotos;
                return true;
            }

            parsedPhotos.Add(new ExternalPropertyPhotoUrlItemDto
            {
                Url = TryGetTrimmedString(photoElement, "url") ?? string.Empty,
                SortOrder = TryGetInt(photoElement, "sortOrder", out var sortOrder) ? sortOrder : 0
            });
            index++;
        }

        var (isValid, validationError) = ExternalPropertyPhotosValidator.ValidatePhotos(parsedPhotos);
        if (!isValid)
        {
            errorMessage = validationError;
            photos = parsedPhotos;
            return true;
        }

        photos = parsedPhotos;
        return true;
    }

    public async Task<(ExternalPropertyPhotoImportCreatedResponseDto? PhotoImport, string? ErrorMessage, string? Detail)> SyncPhotosAsync(
        ExternalPropertyKeyDto keys,
        Property property,
        IReadOnlyList<ExternalPropertyPhotoUrlItemDto> desiredPhotos)
    {
        try
        {
            var listingScope = BuildListingPhotoScope(property.OfficeName, property.PropertyCode);
            var desiredByUrl = desiredPhotos
                .GroupBy(photo => NormalizePhotoUrl(photo.Url))
                .ToDictionary(group => group.Key, group => group.Last());

            var existingPhotos = (await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(property.PropertyId)).ToList();
            var importItems = await GetPropertyPhotoImportHistoryAsync(property.PropertyId);

            var urlToPhotoId = BuildUrlToPhotoIdMap(importItems);
            var photoIdToUrl = urlToPhotoId
                .GroupBy(pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.First().Key);
            var pendingOrInProgressUrls = importItems
                .Where(item => item.Status is PropertyPhotoImportItemStatus.Pending or PropertyPhotoImportItemStatus.InProgress)
                .Select(item => NormalizePhotoUrl(item.Url))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var removedCount = 0;
            foreach (var existingPhoto in existingPhotos)
            {
                if (!photoIdToUrl.TryGetValue(existingPhoto.PhotoId, out var existingUrl)
                    || !desiredByUrl.ContainsKey(existingUrl))
                {
                    await DeletePropertyPhotoAsync(property.OrganizationId, listingScope, existingPhoto);
                    removedCount++;
                }
            }

            var reorderedCount = 0;
            var photosToQueue = new List<ExternalPropertyPhotoUrlItemDto>();
            foreach (var desiredPhoto in desiredPhotos)
            {
                var normalizedUrl = NormalizePhotoUrl(desiredPhoto.Url);
                if (urlToPhotoId.TryGetValue(normalizedUrl, out var photoId))
                {
                    var existingPhoto = existingPhotos.FirstOrDefault(photo => photo.PhotoId == photoId);
                    if (existingPhoto != null && existingPhoto.Order != desiredPhoto.SortOrder)
                    {
                        await _propertyRepository.UpdatePropertyPhotoOrderAsync(photoId, desiredPhoto.SortOrder);
                        reorderedCount++;
                    }

                    continue;
                }

                if (pendingOrInProgressUrls.Contains(normalizedUrl))
                    continue;

                photosToQueue.Add(desiredPhoto);
            }

            ExternalPropertyPhotoImportCreatedResponseDto? photoImport = null;
            if (photosToQueue.Count > 0)
            {
                var (queuedImport, queueError) = await QueuePhotoImportInternalAsync(keys, property.PropertyId, photosToQueue);
                if (queueError != null)
                    return (null, queueError, null);

                photoImport = queuedImport;
            }

            var detail = BuildSyncDetail(removedCount, reorderedCount, photoImport);
            return (photoImport, null, detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error synchronizing external property photos. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}, PhotoCount={PhotoCount}",
                keys.OrganizationId,
                keys.OfficeId,
                keys.PropertyCode,
                keys.VendorId,
                desiredPhotos.Count);
            return (null, "An error occurred while synchronizing property photos", null);
        }
    }

    private async Task<(ExternalPropertyPhotoImportCreatedResponseDto? PhotoImport, string? ErrorMessage)> QueuePhotoImportInternalAsync(
        ExternalPropertyKeyDto keys,
        Guid propertyId,
        IReadOnlyList<ExternalPropertyPhotoUrlItemDto> photos)
    {
        try
        {
            var importId = Guid.NewGuid();
            var import = new PropertyPhotoImport
            {
                ImportId = importId,
                OrganizationId = keys.OrganizationId,
                OfficeId = keys.OfficeId,
                VendorId = keys.VendorId,
                PropertyId = propertyId,
                PropertyCode = keys.PropertyCode,
                Status = PropertyPhotoImportStatus.Pending
            };

            var items = photos
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

            await _uploadLogService.LogPhotoImportQueuedAsync(
                keys.OrganizationId,
                keys.OfficeId,
                keys.VendorId,
                propertyId,
                keys.PropertyCode,
                importId,
                items.Count);

            return (new ExternalPropertyPhotoImportCreatedResponseDto
            {
                ImportId = importId,
                PropertyCode = keys.PropertyCode,
                PropertyId = propertyId,
                Status = PropertyPhotoImportStatus.Pending.ToString(),
                PhotoCount = items.Count
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error queueing external property photo import. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}, PhotoCount={PhotoCount}",
                keys.OrganizationId,
                keys.OfficeId,
                keys.PropertyCode,
                keys.VendorId,
                photos.Count);
            return (null, "An error occurred while queueing property photos");
        }
    }

    private async Task<List<PropertyPhotoImportItem>> GetPropertyPhotoImportHistoryAsync(Guid propertyId)
    {
        try
        {
            return (await _propertyRepository.GetPropertyPhotoImportItemsByPropertyIdAsync(propertyId)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unable to load property photo import history. PropertyId={PropertyId}",
                propertyId);
            return [];
        }
    }

    private async Task DeletePropertyPhotoAsync(Guid organizationId, string listingScope, PropertyPhoto photo)
    {
        if (!string.IsNullOrWhiteSpace(photo.PhotoPath))
            await _fileService.DeleteImageAsync(organizationId, listingScope, photo.PhotoPath, ImageType.Photos);

        await _propertyRepository.DeletePropertyPhotoByIdAsync(photo.PhotoId);
    }

    private static Dictionary<string, int> BuildUrlToPhotoIdMap(IEnumerable<PropertyPhotoImportItem> importItems)
    {
        var urlToPhotoId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in importItems
                     .Where(item => item.Status == PropertyPhotoImportItemStatus.Completed && item.PhotoId.HasValue)
                     .OrderByDescending(item => item.CompletedOn ?? DateTimeOffset.MinValue))
        {
            var normalizedUrl = NormalizePhotoUrl(item.Url);
            urlToPhotoId.TryAdd(normalizedUrl, item.PhotoId!.Value);
        }

        return urlToPhotoId;
    }

    private static string BuildSyncDetail(int removedCount, int reorderedCount, ExternalPropertyPhotoImportCreatedResponseDto? photoImport)
    {
        var parts = new List<string>();
        if (removedCount > 0)
            parts.Add($"{removedCount} removed");

        if (reorderedCount > 0)
            parts.Add($"{reorderedCount} reordered");

        if (photoImport != null)
            parts.Add($"{photoImport.PhotoCount} queued (ImportId={photoImport.ImportId})");

        return parts.Count == 0 ? "Photos synchronized." : $"Photos synchronized: {string.Join(", ", parts)}.";
    }

    private static string NormalizePhotoUrl(string url) => url.Trim();

    private static string BuildListingPhotoScope(string? officeName, string? propertyCode)
    {
        var normalizedOffice = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
        var normalizedCode = string.IsNullOrWhiteSpace(propertyCode) ? "unknown-property" : propertyCode.Trim();
        return $"{normalizedOffice}/listings/{normalizedCode}";
    }

    private static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
    {
        foreach (var property in body.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? TryGetTrimmedString(JsonElement body, string name)
    {
        if (!TryGetProperty(body, name, out var element) || element.ValueKind != JsonValueKind.String)
            return null;

        var value = element.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool TryGetInt(JsonElement body, string name, out int value)
    {
        value = 0;
        if (!TryGetProperty(body, name, out var element) || element.ValueKind != JsonValueKind.Number)
            return false;

        return element.TryGetInt32(out value);
    }
}

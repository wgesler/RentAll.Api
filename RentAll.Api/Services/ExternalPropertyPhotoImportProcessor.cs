using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Services;

public class ExternalPropertyPhotoImportProcessor
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly ExternalPropertyPhotoUrlImporter _urlImporter;
    private readonly ExternalPropertyUploadLogService _uploadLogService;
    private readonly ILogger<ExternalPropertyPhotoImportProcessor> _logger;

    public ExternalPropertyPhotoImportProcessor(
        IPropertyRepository propertyRepository,
        ExternalPropertyPhotoUrlImporter urlImporter,
        ExternalPropertyUploadLogService uploadLogService,
        ILogger<ExternalPropertyPhotoImportProcessor> logger)
    {
        _propertyRepository = propertyRepository;
        _urlImporter = urlImporter;
        _uploadLogService = uploadLogService;
        _logger = logger;
    }

    public async Task<bool> ProcessNextItemAsync(CancellationToken cancellationToken)
    {
        var claim = await _propertyRepository.ClaimNextPropertyPhotoImportItemAsync();
        if (claim == null)
            return false;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var listingScope = BuildListingPhotoScope(claim.OfficeName, claim.PropertyCode);
            var (photoPath, downloadError) = await _urlImporter.DownloadAndSaveAsync(claim.OrganizationId, listingScope, claim.Item.Url);
            if (string.IsNullOrWhiteSpace(photoPath))
            {
                var errorMessage = downloadError ?? "Unable to save photo file";
                await _propertyRepository.CompletePropertyPhotoImportItemAsync(
                    claim.Item.ImportItemId,
                    PropertyPhotoImportItemStatus.Failed,
                    null,
                    errorMessage);
                await _uploadLogService.LogPhotoFailedAsync(
                    claim.OrganizationId,
                    claim.OfficeId,
                    claim.VendorId,
                    claim.PropertyId,
                    claim.PropertyCode,
                    claim.Item.ImportId,
                    claim.Item.SortOrder,
                    claim.Item.Url,
                    errorMessage);
                await TryLogPhotoImportFinishedAsync(claim);
                return true;
            }

            var photo = new PropertyPhoto
            {
                PropertyId = claim.PropertyId,
                Order = claim.Item.SortOrder,
                PhotoPath = photoPath
            };

            var created = await _propertyRepository.CreatePropertyPhotoAsync(photo);
            await _propertyRepository.CompletePropertyPhotoImportItemAsync(
                claim.Item.ImportItemId,
                PropertyPhotoImportItemStatus.Completed,
                created.PhotoId,
                null);
            await _uploadLogService.LogPhotoUploadedAsync(
                claim.OrganizationId,
                claim.OfficeId,
                claim.VendorId,
                claim.PropertyId,
                claim.PropertyCode,
                claim.Item.ImportId,
                created.PhotoId,
                claim.Item.SortOrder,
                claim.Item.Url);
            await TryLogPhotoImportFinishedAsync(claim);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing external property photo import item. ImportItemId={ImportItemId}, ImportId={ImportId}, Url={PhotoUrl}",
                claim.Item.ImportItemId,
                claim.Item.ImportId,
                claim.Item.Url);

            const string errorMessage = "Unable to add photo";
            await _propertyRepository.CompletePropertyPhotoImportItemAsync(
                claim.Item.ImportItemId,
                PropertyPhotoImportItemStatus.Failed,
                null,
                errorMessage);
            await _uploadLogService.LogPhotoFailedAsync(
                claim.OrganizationId,
                claim.OfficeId,
                claim.VendorId,
                claim.PropertyId,
                claim.PropertyCode,
                claim.Item.ImportId,
                claim.Item.SortOrder,
                claim.Item.Url,
                errorMessage);
            await TryLogPhotoImportFinishedAsync(claim);
        }

        return true;
    }

    private async Task TryLogPhotoImportFinishedAsync(PropertyPhotoImportClaim claim)
    {
        var items = (await _propertyRepository.GetPropertyPhotoImportItemsByImportIdAsync(claim.Item.ImportId)).ToList();
        if (items.Any(x => x.Status is PropertyPhotoImportItemStatus.Pending or PropertyPhotoImportItemStatus.InProgress))
            return;

        var completedCount = items.Count(x => x.Status == PropertyPhotoImportItemStatus.Completed);
        var failedCount = items.Count(x => x.Status == PropertyPhotoImportItemStatus.Failed);
        var status = failedCount == 0
            ? PropertyUploadLogStatuses.Success
            : completedCount == 0
                ? PropertyUploadLogStatuses.Failed
                : PropertyUploadLogStatuses.Partial;

        await _uploadLogService.LogPhotoImportFinishedAsync(
            claim.OrganizationId,
            claim.OfficeId,
            claim.VendorId,
            claim.PropertyId,
            claim.PropertyCode,
            claim.Item.ImportId,
            status,
            completedCount,
            failedCount);
    }

    private static string BuildListingPhotoScope(string? officeName, string? propertyCode)
    {
        var normalizedOffice = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
        var normalizedCode = string.IsNullOrWhiteSpace(propertyCode) ? "unknown-property" : propertyCode.Trim();
        return $"{normalizedOffice}/listings/{normalizedCode}";
    }
}

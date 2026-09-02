using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Services;

public static class PropertyUploadLogEvents
{
    public const string Property = "Property";
    public const string PhotoImport = "PhotoImport";
    public const string Photo = "Photo";
}

public static class PropertyUploadLogStatuses
{
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Partial = "Partial";
    public const string Pending = "Pending";
}

public class ExternalPropertyUploadLogService
{
    private const int MaxUrlLength = 2048;
    private const int MaxMessageLength = 2500;
    private readonly ILoggingRepository _loggingRepository;

    public ExternalPropertyUploadLogService(ILoggingRepository loggingRepository)
    {
        _loggingRepository = loggingRepository;
    }

    public Task LogPropertyCreatedAsync(Guid organizationId, int officeId, Guid vendorId, Guid propertyId, string propertyCode)
    {
        return AddAsync(new PropertyUploadLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyId = propertyId,
            PropertyCode = propertyCode,
            EventType = PropertyUploadLogEvents.Property,
            Status = PropertyUploadLogStatuses.Success,
            Message = TruncateMessage($"Property {propertyCode} created via external API.")
        });
    }

    public Task LogPropertyUpdatedAsync(Guid organizationId, int officeId, Guid vendorId, Guid propertyId, string propertyCode)
    {
        return AddAsync(new PropertyUploadLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyId = propertyId,
            PropertyCode = propertyCode,
            EventType = PropertyUploadLogEvents.Property,
            Status = PropertyUploadLogStatuses.Success,
            Message = TruncateMessage($"Property {propertyCode} updated via external API.")
        });
    }

    public Task LogPhotoImportQueuedAsync(Guid organizationId, int officeId, Guid vendorId, Guid propertyId, string propertyCode, Guid importId, int photoCount)
    {
        return AddAsync(new PropertyUploadLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyId = propertyId,
            PropertyCode = propertyCode,
            EventType = PropertyUploadLogEvents.PhotoImport,
            Status = PropertyUploadLogStatuses.Pending,
            ImportId = importId,
            Message = TruncateMessage($"Photo import queued for {propertyCode}: {photoCount} URL(s). ImportId={importId}.")
        });
    }

    public Task LogPhotoUploadedAsync(Guid organizationId, int officeId, Guid vendorId, Guid propertyId, string propertyCode, Guid importId, int photoId, int sortOrder, string url)
    {
        return AddAsync(new PropertyUploadLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyId = propertyId,
            PropertyCode = propertyCode,
            EventType = PropertyUploadLogEvents.Photo,
            Status = PropertyUploadLogStatuses.Success,
            ImportId = importId,
            PhotoId = photoId,
            Url = TruncateUrl(url),
            Message = TruncateMessage($"Photo uploaded for {propertyCode} (sort order {sortOrder}, photoId {photoId}).")
        });
    }

    public Task LogPhotoFailedAsync(Guid organizationId, int officeId, Guid vendorId, Guid propertyId, string propertyCode, Guid importId, int sortOrder, string url, string errorMessage)
    {
        return AddAsync(new PropertyUploadLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyId = propertyId,
            PropertyCode = propertyCode,
            EventType = PropertyUploadLogEvents.Photo,
            Status = PropertyUploadLogStatuses.Failed,
            ImportId = importId,
            Url = TruncateUrl(url),
            Message = TruncateMessage($"Photo failed for {propertyCode} (sort order {sortOrder}): {errorMessage}")
        });
    }

    public Task LogPhotoImportFinishedAsync(Guid organizationId, int officeId, Guid vendorId, Guid propertyId, string propertyCode, Guid importId, string status, int completedCount, int failedCount)
    {
        return AddAsync(new PropertyUploadLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyId = propertyId,
            PropertyCode = propertyCode,
            EventType = PropertyUploadLogEvents.PhotoImport,
            Status = status,
            ImportId = importId,
            Message = TruncateMessage($"Photo import finished for {propertyCode}: {completedCount} succeeded, {failedCount} failed. ImportId={importId}.")
        });
    }

    private Task AddAsync(PropertyUploadLog log)
    {
        return _loggingRepository.AddPropertyUploadLogAsync(log);
    }

    private static string TruncateMessage(string message)
    {
        return message.Length <= MaxMessageLength ? message : message[..MaxMessageLength];
    }

    private static string TruncateUrl(string url)
    {
        var trimmed = url.Trim();
        return trimmed.Length <= MaxUrlLength ? trimmed : trimmed[..MaxUrlLength];
    }
}

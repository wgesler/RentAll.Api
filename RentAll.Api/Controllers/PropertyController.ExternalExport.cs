using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Properties.Properties;
using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    [AllowAnonymous]
    [HttpGet("external")]
    public async Task<IActionResult> GetExternalPropertiesAsync([FromQuery] ExternalPropertyExportQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var (isValid, errorMessage) = query.Validate();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var accessError = await ValidateExternalPropertyAccessAsync(query.OrganizationId, query.OfficeId);
        if (accessError != null)
            return accessError;

        try
        {
            var exportProperties = (await _propertyRepository.GetExternalExportListByOrganizationIdAsync(query.OrganizationId))
                .Where(property => property.OfficeId == query.OfficeId)
                .ToList();

            if (await HasPartnerIntegrationAccessAsync(query.OrganizationId))
            {
                var partnerProperties = (await _partnerRepository.GetExternalExportListAsync())
                    .Where(property => property.OfficeId == query.OfficeId);
                exportProperties.AddRange(partnerProperties);
            }

            var primaryPhotos = (await _propertyRepository.GetPrimaryPropertyPhotosByPropertyIdsAsync(
                exportProperties.Select(property => property.PropertyId).ToList()))
                .ToDictionary(photo => photo.PropertyId);

            var properties = new List<ExternalPropertyExportItemDto>();
            foreach (var exportProperty in exportProperties.OrderBy(property => property.PropertyCode))
            {
                var propertyDto = ExternalPropertyExportItemDto.FromExportList(exportProperty);
                if (primaryPhotos.TryGetValue(exportProperty.PropertyId, out var primaryPhoto))
                {
                    propertyDto.PrimaryPhotoUrl = await ResolveExternalPrimaryPhotoUrlAsync(
                        exportProperty.OrganizationId,
                        exportProperty.OfficeName,
                        exportProperty.PropertyCode,
                        primaryPhoto);
                }

                properties.Add(propertyDto);
            }

            return Ok(new ExternalPropertyExportResponseDto
            {
                OrganizationId = query.OrganizationId,
                OfficeId = query.OfficeId,
                Properties = properties
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting external property export list. OrganizationId={OrganizationId}, OfficeId={OfficeId}",
                query.OrganizationId,
                query.OfficeId);
            return ServerError("An error occurred while retrieving properties");
        }
    }

    [AllowAnonymous]
    [HttpGet("external/{propertyCode}")]
    public async Task<IActionResult> GetExternalPropertyByCodeAsync(
        string propertyCode,
        [FromQuery] ExternalPropertyExportQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var (isValid, errorMessage) = query.Validate();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (string.IsNullOrWhiteSpace(propertyCode))
            return BadRequest("PropertyCode is required");

        var accessError = await ValidateExternalPropertyAccessAsync(query.OrganizationId, query.OfficeId);
        if (accessError != null)
            return accessError;

        try
        {
            var includePartners = await HasPartnerIntegrationAccessAsync(query.OrganizationId);
            var property = await ResolveExternalExportPropertyAsync(query.OrganizationId, propertyCode.Trim(), includePartners);
            if (property == null || property.OfficeId != query.OfficeId)
                return NotFound("Property not found");

            var propertyDto = ExternalPropertyExportItemDto.FromProperty(property);
            var primaryPhotos = await _propertyRepository.GetPrimaryPropertyPhotosByPropertyIdsAsync([property.PropertyId]);
            var primaryPhoto = primaryPhotos.FirstOrDefault();
            if (primaryPhoto != null)
            {
                propertyDto.PrimaryPhotoUrl = await ResolveExternalPrimaryPhotoUrlAsync(
                    property.OrganizationId,
                    property.OfficeName,
                    property.PropertyCode,
                    primaryPhoto);
            }

            return Ok(new ExternalPropertyExportResponseDto
            {
                OrganizationId = query.OrganizationId,
                OfficeId = query.OfficeId,
                Properties = [propertyDto]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting external property export detail. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}",
                query.OrganizationId,
                query.OfficeId,
                propertyCode);
            return ServerError("An error occurred while retrieving the property");
        }
    }

    [AllowAnonymous]
    [HttpGet("external/{propertyCode}/photos")]
    public async Task<IActionResult> GetExternalPropertyPhotosAsync(
        string propertyCode,
        [FromQuery] ExternalPropertyOrganizationQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var (isValid, errorMessage) = query.Validate();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (string.IsNullOrWhiteSpace(propertyCode))
            return BadRequest("PropertyCode is required");

        var accessError = await ValidateExternalPropertyOrganizationAccessAsync(query.OrganizationId);
        if (accessError != null)
            return accessError;

        try
        {
            var includePartners = await HasPartnerIntegrationAccessAsync(query.OrganizationId);
            var property = await ResolveExternalExportPropertyAsync(query.OrganizationId, propertyCode.Trim(), includePartners);
            if (property == null)
                return NotFound("Property not found");

            var photos = await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(property.PropertyId);
            var photoItems = new List<ExternalPropertyPhotoUrlItemDto>();

            foreach (var photo in photos.OrderBy(item => item.Order).ThenBy(item => item.PhotoId))
            {
                var url = await ResolveExternalPrimaryPhotoUrlAsync(
                    property.OrganizationId,
                    property.OfficeName,
                    property.PropertyCode,
                    photo);
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                photoItems.Add(new ExternalPropertyPhotoUrlItemDto
                {
                    Url = url,
                    SortOrder = photo.Order
                });
            }

            return Ok(new ExternalPropertyPhotosExportResponseDto
            {
                PropertyCode = property.PropertyCode,
                Photos = photoItems
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting external property photos. OrganizationId={OrganizationId}, PropertyCode={PropertyCode}",
                query.OrganizationId,
                propertyCode);
            return ServerError("An error occurred while retrieving property photos");
        }
    }

    private async Task<bool> HasPartnerIntegrationAccessAsync(Guid organizationId)
    {
        var features = await _organizationRepository.GetFeaturesByOrganizationIdAsync(organizationId);
        return features.Any(feature =>
            feature.FeatureTypeId == FeatureType.PartnerIntegration
            && feature.HasAccess);
    }

    private async Task<Property?> ResolveExternalExportPropertyAsync(
        Guid requestingOrganizationId,
        string propertyCode,
        bool includePartners)
    {
        var organizationProperty = await _propertyRepository.GetPropertyByCodeAsync(propertyCode, requestingOrganizationId);
        if (IsExternalExportEligible(organizationProperty))
            return organizationProperty;

        if (!includePartners)
            return null;

        return await _propertyRepository.GetPartnerExternalExportByCodeAsync(propertyCode);
    }

    private static bool IsExternalExportEligible(Property? property) =>
        property != null
        && property.IsActive
        && !property.OfflineChecked;

    private async Task<string?> ResolveExternalPrimaryPhotoUrlAsync(
        Guid organizationId,
        string? officeName,
        string propertyCode,
        PropertyPhoto primaryPhoto)
    {
        var listingScope = BuildListingPhotoScope(officeName, propertyCode);
        var fileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
            organizationId,
            listingScope,
            primaryPhoto.PhotoPath,
            ImageType.Photos);

        if (fileDetails != null && !string.IsNullOrWhiteSpace(fileDetails.DataUrl))
            return fileDetails.DataUrl;

        if (string.IsNullOrWhiteSpace(primaryPhoto.PhotoPath))
            return null;

        var normalizedPath = primaryPhoto.PhotoPath.Trim().Replace("\\", "/");
        if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        if (!normalizedPath.StartsWith('/'))
            normalizedPath = "/" + normalizedPath;

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        return $"{baseUrl}{normalizedPath}";
    }
}

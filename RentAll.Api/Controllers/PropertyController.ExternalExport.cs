using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Properties.Properties;
using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    [AllowAnonymous]
    [HttpGet("external")]
    public async Task<IActionResult> GetExternalPropertiesAsync([FromQuery] ExternalPropertyOrganizationQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var (isValid, errorMessage) = query.Validate();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var accessError = await ValidateExternalPropertyOrganizationAccessAsync(query.OrganizationId);
        if (accessError != null)
            return accessError;

        try
        {
            var properties = (await _propertyRepository.GetExternalExportListByOrganizationIdAsync(query.OrganizationId))
                .Select(property => new ExternalPropertyListResponseDto(property))
                .ToList();

            if (await HasPartnerIntegrationAccessAsync(query.OrganizationId))
            {
                var partnerProperties = await _partnerRepository.GetExternalExportListAsync();
                properties.AddRange(partnerProperties.Select(property => new ExternalPropertyListResponseDto(property)));
            }

            return Ok(properties.OrderBy(property => property.PropertyCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external property export list. OrganizationId={OrganizationId}", query.OrganizationId);
            return ServerError("An error occurred while retrieving properties");
        }
    }

    [AllowAnonymous]
    [HttpGet("external/{propertyCode}")]
    public async Task<IActionResult> GetExternalPropertyByCodeAsync(
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

            return Ok(new ExternalPropertyResponseDto(property));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting external property export detail. OrganizationId={OrganizationId}, PropertyCode={PropertyCode}",
                query.OrganizationId,
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

            var listingScope = BuildListingPhotoScope(property.OfficeName, property.PropertyCode);
            var photos = await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(property.PropertyId);
            var response = new List<PropertyPhotoResponseDto>();

            foreach (var photo in photos)
            {
                var photoResponse = new PropertyPhotoResponseDto(photo);
                photoResponse.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    property.OrganizationId,
                    listingScope,
                    photo.PhotoPath,
                    ImageType.Photos);
                response.Add(photoResponse);
            }

            return Ok(response);
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
}

using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Partners;
using RentAll.Api.Dtos.Properties.Properties;
using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/partner")]
[Authorize]
public class PartnerController : BaseController
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IFileAttachmentHelper _fileAttachmentHelper;
    private readonly ILogger<PartnerController> _logger;

    public PartnerController(
        IPartnerRepository partnerRepository,
        IPropertyRepository propertyRepository,
        IFileAttachmentHelper fileAttachmentHelper,
        ILogger<PartnerController> logger)
    {
        _partnerRepository = partnerRepository;
        _propertyRepository = propertyRepository;
        _fileAttachmentHelper = fileAttachmentHelper;
        _logger = logger;
    }

    [HttpGet("properties")]
    public async Task<IActionResult> GetAllProperties()
    {
        try
        {
            var properties = await _partnerRepository.GetAllPropertiesAsync();
            return Ok(properties.Select(p => new PropertyListResponseDto(p)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner properties");
            return ServerError("An error occurred while retrieving partner properties");
        }
    }

    [HttpGet("properties/user/{userId:guid}/active")]
    public async Task<IActionResult> GetActivePropertiesByUserSelection(Guid userId)
    {
        if (CurrentUser == Guid.Empty || CurrentUser != userId)
            return Unauthorized();

        try
        {
            var properties = await _partnerRepository.GetActivePropertyListBySelectionCriteriaAsync(CurrentUser);
            return Ok(properties.Select(p => new PropertyListResponseDto(p)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner properties by selection for user: {UserId}", CurrentUser);
            return ServerError("An error occurred while retrieving partner properties");
        }
    }

    [HttpGet("properties/{propertyId:guid}")]
    public async Task<IActionResult> GetPropertyById(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var property = await _propertyRepository.GetPartnerPropertyByIdAsync(propertyId);
            if (property == null)
                return NotFound("Partner property not found");

            return Ok(new PropertyResponseDto(property));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner property {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving the partner property");
        }
    }

    [HttpGet("properties/{propertyId:guid}/photos")]
    public async Task<IActionResult> GetPropertyPhotosByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var property = await _propertyRepository.GetPartnerPropertyByIdAsync(propertyId);
            if (property == null)
                return NotFound("Partner property not found");

            var listingScope = BuildListingPhotoScope(property.OfficeName, property.PropertyCode);
            var photos = await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(propertyId);
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
            _logger.LogError(ex, "Error getting partner property photos {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving partner property photos");
        }
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetListOfCities()
    {
        try
        {
            var cities = await _partnerRepository.GetListOfCitiesAsync();
            return Ok(cities.Select(c => new PartnerCityStateResponseDto(c)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner property cities");
            return ServerError("An error occurred while retrieving partner property cities");
        }
    }

    [HttpGet("contact/{propertyId:guid}")]
    public async Task<IActionResult> GetPartnerContact(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var contact = await _partnerRepository.GetPartnerContactAsync(propertyId);
            if (contact == null)
                return NotFound("Partner property contact not found");

            return Ok(new PartnerContactResponseDto(contact));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner contact for property {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving partner property contact");
        }
    }

    private static string BuildListingPhotoScope(string? officeName, string? propertyCode)
    {
        var normalizedOffice = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
        var normalizedCode = string.IsNullOrWhiteSpace(propertyCode) ? "unknown-property" : propertyCode.Trim();
        return $"{normalizedOffice}/listings/{normalizedCode}";
    }
}

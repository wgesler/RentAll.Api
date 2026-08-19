using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Partners;
using RentAll.Api.Dtos.Properties.Properties;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/partner")]
[Authorize]
public class PartnerController : BaseController
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly ILogger<PartnerController> _logger;

    public PartnerController(
        IPartnerRepository partnerRepository,
        ILogger<PartnerController> logger)
    {
        _partnerRepository = partnerRepository;
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
}

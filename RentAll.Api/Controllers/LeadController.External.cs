using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Api.Dtos.Leads.Owners;
using RentAll.Api.Dtos.Leads.Rentals;
using RentAll.Domain.Configuration;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Rentals

    [AllowAnonymous]
    [HttpPost("external/rentals")]
    public async Task<IActionResult> CreateExternalRentalLeadAsync(
        [FromBody] CreateExternalLeadRentalDto dto,
        [FromServices] IOptions<ExternalLeadIntakeSettings> settings)
    {
        if (dto == null)
            return BadRequest("Rental lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!IsExternalLeadApiKeyValid(settings.Value.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
            if (organization == null)
                return BadRequest("Invalid OrganizationId");

            if (!await CanAssignAgentForOrganizationAsync(dto.OrganizationId, dto.AgentId))
                return BadRequest("AgentId is not valid for the specified organization.");

            var created = await _leadRepository.CreateRentalAsync(dto.ToModel());
            return Ok(new LeadRentalResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external rental lead intake request");
            return ServerError("An error occurred while creating the rental lead");
        }
    }
    #endrgion

    #region Owners

    [AllowAnonymous]
    [HttpPost("external/owners")]
    public async Task<IActionResult> CreateExternalOwnerLeadAsync(
        [FromBody] CreateExternalLeadOwnerDto dto,
        [FromServices] IOptions<ExternalLeadIntakeSettings> settings)
    {
        if (dto == null)
            return BadRequest("Owner lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!IsExternalLeadApiKeyValid(settings.Value.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
            if (organization == null)
                return BadRequest("Invalid OrganizationId");

            if (!await CanAssignAgentForOrganizationAsync(dto.OrganizationId, dto.AgentId))
                return BadRequest("AgentId is not valid for the specified organization.");

            var created = await _leadRepository.CreateOwnerAsync(dto.ToModel());
            return Ok(new LeadOwnerResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external owner lead intake request");
            return ServerError("An error occurred while creating the owner lead");
        }
    }

    private bool IsExternalLeadApiKeyValid(string configuredApiKey)
    {
        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return false;

        var inboundApiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(inboundApiKey))
            return false;

        return string.Equals(inboundApiKey.Trim(), configuredApiKey.Trim(), StringComparison.Ordinal);
    }

    #endregion
}

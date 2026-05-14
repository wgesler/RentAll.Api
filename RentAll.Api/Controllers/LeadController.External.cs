using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Api.Dtos.Leads.General;
using RentAll.Api.Dtos.Leads.Owners;
using RentAll.Api.Dtos.Leads.Rentals;
using RentAll.Domain.Configuration;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region General

    [AllowAnonymous]
    [HttpPost("external/general")]
    public async Task<IActionResult> CreateExternalGeneralLeadAsync(
        [FromBody] CreateExternalLeadGeneralDto dto,
        [FromServices] IOptions<ExternalLeadIntakeSettings> settings)
    {
        if (dto == null)
            return BadRequest("General lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!IsExternalLeadApiKeyValid(settings.Value.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            var orgOfficeError = await TryValidateExternalLeadOrgAndOfficeAsync(dto.OrganizationId, dto.OfficeId);
            if (orgOfficeError != null)
                return orgOfficeError;

            var created = await _leadRepository.CreateGeneralAsync(dto.ToModel(dto.OrganizationId));
            return Ok(new LeadGeneralResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external general lead intake request");
            return ServerError("An error occurred while creating the general lead");
        }
    }

    #endregion

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
            var orgOfficeError = await TryValidateExternalLeadOrgAndOfficeAsync(dto.OrganizationId, dto.OfficeId);
            if (orgOfficeError != null)
                return orgOfficeError;

            var created = await _leadRepository.CreateRentalAsync(dto.ToModel(dto.OrganizationId));
            return Ok(new LeadRentalResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external rental lead intake request");
            return ServerError("An error occurred while creating the rental lead");
        }
    }

    #endregion

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
            var orgOfficeError = await TryValidateExternalLeadOrgAndOfficeAsync(dto.OrganizationId, dto.OfficeId);
            if (orgOfficeError != null)
                return orgOfficeError;

            var created = await _leadRepository.CreateOwnerAsync(dto.ToModel(dto.OrganizationId));
            return Ok(new LeadOwnerResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external owner lead intake request");
            return ServerError("An error occurred while creating the owner lead");
        }
    }

    private async Task<IActionResult?> TryValidateExternalLeadOrgAndOfficeAsync(Guid organizationId, int officeId)
    {
        var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        if (office == null)
            return BadRequest("Invalid OfficeId for OrganizationId.");

        return null;
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

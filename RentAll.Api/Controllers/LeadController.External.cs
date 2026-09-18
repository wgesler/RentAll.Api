using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Leads.General;
using RentAll.Api.Dtos.Leads.Partners;
using RentAll.Api.Dtos.Leads.Rentals;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region General

    [AllowAnonymous]
    [HttpPost("external/general")]
    public async Task<IActionResult> CreateExternalGeneralLeadAsync([FromBody] JsonElement body)
    {
        var (parsed, dto, parseError) = CreateExternalLeadGeneralDto.TryParseFromBody(body);
        if (!parsed || dto == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        try
        {
            var orgOfficeError = await TryValidateExternalLeadOfficeAsync(organization, dto.OfficeId);
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
    public async Task<IActionResult> CreateExternalRentalLeadAsync([FromBody] JsonElement body)
    {
        var (parsed, dto, parseError) = CreateExternalLeadRentalDto.TryParseFromBody(body);
        if (!parsed || dto == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        try
        {
            var orgOfficeError = await TryValidateExternalLeadOfficeAsync(organization, dto.OfficeId);
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

    #region Partners

    [AllowAnonymous]
    [HttpPost("external/partners")]
    public async Task<IActionResult> CreateExternalPartnerLeadAsync([FromBody] JsonElement body)
    {
        var (parsed, dto, parseError) = CreateExternalLeadPartnerDto.TryParseFromBody(body);
        if (!parsed || dto == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        try
        {
            var orgOfficeError = await TryValidateExternalLeadOfficeAsync(organization, dto.OfficeId);
            if (orgOfficeError != null)
                return orgOfficeError;

            var created = await _leadRepository.CreatePartnerAsync(dto.ToModel(dto.OrganizationId));
            return Ok(new LeadPartnerResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external partner lead intake request");
            return ServerError("An error occurred while creating the partner lead");
        }
    }

    #endregion

    #region Owners

    [AllowAnonymous]
    [HttpPost("external/owners")]
    public async Task<IActionResult> CreateExternalOwnerLeadAsync([FromBody] JsonElement body)
    {
        var (parsed, dto, parseError) = CreateExternalLeadOwnerDto.TryParseFromBody(body);
        if (!parsed || dto == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        try
        {
            var orgOfficeError = await TryValidateExternalLeadOfficeAsync(organization, dto.OfficeId);
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
    #endregion

    #region Private Support Methods
    private async Task<IActionResult?> TryValidateExternalLeadOfficeAsync(Organization organization, int officeId)
    {
        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organization.OrganizationId);
        if (office == null)
            return BadRequest("Invalid OfficeId for OrganizationId.");

        return null;
    }

    #endregion
}

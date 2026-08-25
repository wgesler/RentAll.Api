using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Leads.General;
using RentAll.Api.Dtos.Leads.Rentals;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region General

    [AllowAnonymous]
    [HttpPost("external/general")]
    public async Task<IActionResult> CreateExternalGeneralLeadAsync([FromBody] CreateExternalLeadGeneralDto dto)
    {
        if (dto == null)
            return BadRequest("General lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadGeneralKeyVaultSecretName()))
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
    public async Task<IActionResult> CreateExternalRentalLeadAsync([FromBody] CreateExternalLeadRentalDto dto)
    {
        if (dto == null)
            return BadRequest("Rental lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadRentalKeyVaultSecretName()))
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

    #region Owners

    [AllowAnonymous]
    [HttpPost("external/owners")]
    public async Task<IActionResult> CreateExternalOwnerLeadAsync([FromBody] CreateExternalLeadOwnerDto dto)
    {
        if (dto == null)
            return BadRequest("Owner lead data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalLeadOwnerKeyVaultSecretName()))
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

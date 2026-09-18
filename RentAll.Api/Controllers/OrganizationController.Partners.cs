using RentAll.Api.Dtos.Organizations.Organizations;

namespace RentAll.Api.Controllers;

public partial class OrganizationController
{
    [HttpGet("{organizationId:guid}/partners")]
    public async Task<IActionResult> GetPartnerSettings(Guid organizationId)
    {
        var resolvedOrganizationId = ResolvePartnerSettingsOrganizationId(organizationId);
        if (resolvedOrganizationId == null)
            return Unauthorized();

        try
        {
            var organizations = await _organizationRepository.GetOrganizationsWithPartnerFeatureAsync(resolvedOrganizationId.Value);
            var partnersIn = await _organizationRepository.GetPartnersInByOrganizationIdAsync(resolvedOrganizationId.Value);
            var partnersOut = await _organizationRepository.GetPartnersOutByOrganizationIdAsync(resolvedOrganizationId.Value);

            return Ok(new OrganizationPartnerSettingsResponseDto
            {
                PartnerOrganizations = organizations.Select(organization => new OrganizationPartnerOptionResponseDto(organization)).ToList(),
                PartnersIn = partnersIn.ToList(),
                PartnersOut = partnersOut.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner settings for organization {OrganizationId}", resolvedOrganizationId);
            return ServerError("An error occurred while retrieving partner settings");
        }
    }

    [HttpPost("{organizationId:guid}/partners/in/{partnerOrganizationId:guid}")]
    public async Task<IActionResult> AddPartnerIn(Guid organizationId, Guid partnerOrganizationId)
    {
        return await ChangePartnerShareAsync(organizationId, partnerOrganizationId, _organizationRepository.AddPartnerInAsync);
    }

    [HttpDelete("{organizationId:guid}/partners/in/{partnerOrganizationId:guid}")]
    public async Task<IActionResult> DeletePartnerIn(Guid organizationId, Guid partnerOrganizationId)
    {
        return await ChangePartnerShareAsync(organizationId, partnerOrganizationId, _organizationRepository.DeletePartnerInAsync);
    }

    [HttpPost("{organizationId:guid}/partners/out/{partnerOrganizationId:guid}")]
    public async Task<IActionResult> AddPartnerOut(Guid organizationId, Guid partnerOrganizationId)
    {
        return await ChangePartnerShareAsync(organizationId, partnerOrganizationId, _organizationRepository.AddPartnerOutAsync);
    }

    [HttpDelete("{organizationId:guid}/partners/out/{partnerOrganizationId:guid}")]
    public async Task<IActionResult> DeletePartnerOut(Guid organizationId, Guid partnerOrganizationId)
    {
        return await ChangePartnerShareAsync(organizationId, partnerOrganizationId, _organizationRepository.DeletePartnerOutAsync);
    }

    private async Task<IActionResult> ChangePartnerShareAsync(Guid organizationId, Guid partnerOrganizationId, Func<Guid, Guid, Task> change)
    {
        var resolvedOrganizationId = ResolvePartnerSettingsOrganizationId(organizationId);
        if (resolvedOrganizationId == null)
            return Unauthorized();

        if (partnerOrganizationId == Guid.Empty || partnerOrganizationId == resolvedOrganizationId)
            return BadRequest("PartnerOrganizationId is required");

        try
        {
            await change(resolvedOrganizationId.Value, partnerOrganizationId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing partner share for organization {OrganizationId}", resolvedOrganizationId);
            return ServerError("An error occurred while updating partner settings");
        }
    }

    private Guid? ResolvePartnerSettingsOrganizationId(Guid organizationId)
    {
        if (!IsAdmin() && !IsSuperAdmin())
            return null;

        if (IsSuperAdmin())
            return organizationId == Guid.Empty ? CurrentOrganizationId : organizationId;

        if (organizationId != Guid.Empty && organizationId != CurrentOrganizationId)
            return null;

        return CurrentOrganizationId;
    }
}

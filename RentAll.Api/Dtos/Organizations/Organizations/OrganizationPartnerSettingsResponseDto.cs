namespace RentAll.Api.Dtos.Organizations.Organizations;

public class OrganizationPartnerSettingsResponseDto
{
    public List<OrganizationPartnerOptionResponseDto> PartnerOrganizations { get; set; } = [];
    public List<Guid> PartnersIn { get; set; } = [];
    public List<Guid> PartnersOut { get; set; } = [];
}

public class OrganizationPartnerOptionResponseDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    public OrganizationPartnerOptionResponseDto()
    {
    }

    public OrganizationPartnerOptionResponseDto(OrganizationPartnerOption organization)
    {
        OrganizationId = organization.OrganizationId;
        OrganizationCode = organization.OrganizationCode;
        Name = organization.Name;
        DisplayName = organization.DisplayName;
    }
}

namespace RentAll.Infrastructure.Entities.Organizations;

public class OrganizationPartnerOptionEntity
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class PartnerOrganizationIdEntity
{
    public Guid PartnerOrganizationId { get; set; }
}

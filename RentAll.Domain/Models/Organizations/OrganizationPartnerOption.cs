namespace RentAll.Domain.Models;

public class OrganizationPartnerOption
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

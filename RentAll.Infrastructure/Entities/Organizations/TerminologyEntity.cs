namespace RentAll.Infrastructure.Entities.Organizations;

public class TerminologyEntity
{
    public int TermId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
}

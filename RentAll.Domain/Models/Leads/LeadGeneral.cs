using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Leads;

public class LeadGeneral
{
    public int GeneralId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public LeadStateType LeadState { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneMobile { get; set; }
    public string? Message { get; set; }
    public bool IsActive { get; set; } = true;
}

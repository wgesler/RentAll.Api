using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.General;

public class LeadGeneralResponseDto
{
    public int GeneralId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int LeadStateId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? PhoneMobile { get; set; }
    public string? Message { get; set; }
    public bool IsActive { get; set; }

    public LeadGeneralResponseDto(LeadGeneral lead)
    {
        GeneralId = lead.GeneralId;
        OrganizationId = lead.OrganizationId;
        OfficeId = lead.OfficeId;
        LeadStateId = (int)lead.LeadState;
        FirstName = lead.FirstName;
        LastName = lead.LastName;
        Location = lead.Location;
        Email = lead.Email;
        PhoneMobile = lead.PhoneMobile;
        Message = lead.Message;
        IsActive = lead.IsActive;
    }
}

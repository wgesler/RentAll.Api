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
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public LeadGeneralResponseDto(LeadGeneral lead)
    {
        GeneralId = lead.GeneralId;
        OrganizationId = lead.OrganizationId;
        OfficeId = lead.OfficeId;
        LeadStateId = (int)lead.LeadState;
        FirstName = lead.FirstName;
        LastName = lead.LastName;
        Email = lead.Email;
        Phone = lead.PhoneMobile;
        Message = lead.Message;
        Notes = lead.Notes;
        CreatedOn = lead.CreatedOn;
        CreatedBy = lead.CreatedBy;
        ModifiedOn = lead.ModifiedOn;
        ModifiedBy = lead.ModifiedBy;
        ModifiedByName = lead.ModifiedByName;
        IsActive = lead.IsActive;
    }
}

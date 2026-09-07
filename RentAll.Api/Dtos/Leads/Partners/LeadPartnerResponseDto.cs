using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Partners;

public class LeadPartnerResponseDto
{
    public int PartnerId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int LeadStateId { get; set; }
    public string? Name { get; set; }
    public string? CompanyName { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? MarketsCitiesServed { get; set; }
    public string? FurnishedPropertiesInPortfolio { get; set; }
    public string? AboutYourBusiness { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }
    public bool IsActive { get; set; }

    public LeadPartnerResponseDto(LeadPartner partner)
    {
        PartnerId = partner.PartnerId;
        OrganizationId = partner.OrganizationId;
        OfficeId = partner.OfficeId;
        LeadStateId = (int)partner.LeadState;
        Name = partner.Name;
        CompanyName = partner.CompanyName;
        Title = partner.Title;
        Email = partner.Email;
        Phone = partner.Phone;
        MarketsCitiesServed = partner.MarketsCitiesServed;
        FurnishedPropertiesInPortfolio = partner.FurnishedPropertiesInPortfolio;
        AboutYourBusiness = partner.AboutYourBusiness;
        Notes = partner.Notes;
        CreatedOn = partner.CreatedOn;
        CreatedBy = partner.CreatedBy;
        ModifiedOn = partner.ModifiedOn;
        ModifiedBy = partner.ModifiedBy;
        ModifiedByName = partner.ModifiedByName;
        EmailPhoneConsent = partner.EmailPhoneConsent;
        SmsConsent = partner.SmsConsent;
        IsActive = partner.IsActive;
    }
}

using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Leads;

public class LeadPartner
{
    public int PartnerId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public LeadStateType LeadState { get; set; }
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
    public bool IsActive { get; set; } = true;
}

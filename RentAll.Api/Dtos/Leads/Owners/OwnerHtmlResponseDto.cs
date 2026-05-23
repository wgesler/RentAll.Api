using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class OwnerHtmlResponseDto
{
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OwnerAgreement { get; set; } = string.Empty;
    public string DirectDeposit { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public OwnerHtmlResponseDto(OwnerHtml ownerHtml)
    {
        PropertyId = ownerHtml.PropertyId;
        OrganizationId = ownerHtml.OrganizationId;
        OwnerAgreement = ownerHtml.OwnerAgreement;
        DirectDeposit = ownerHtml.DirectDeposit;
        IsDeleted = ownerHtml.IsDeleted;
        CreatedOn = ownerHtml.CreatedOn;
        CreatedBy = ownerHtml.CreatedBy;
        ModifiedOn = ownerHtml.ModifiedOn;
        ModifiedBy = ownerHtml.ModifiedBy;
    }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyHtmls;

public class PropertyHtmlResponseDto
{
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string WelcomeLetter { get; set; } = string.Empty;
    public string InspectionChecklist { get; set; } = string.Empty;
    public string Lease { get; set; } = string.Empty;
    public string Invoice { get; set; } = string.Empty;
    public string LetterOfResponsibility { get; set; } = string.Empty;
    public string NoticeToVacate { get; set; } = string.Empty;
    public string CreditAuthorization { get; set; } = string.Empty;
    public string CreditApplicationBusiness { get; set; } = string.Empty;
    public string CreditApplicationIndividual { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public PropertyHtmlResponseDto(PropertyHtml propertyHtml)
    {
        PropertyId = propertyHtml.PropertyId;
        OrganizationId = propertyHtml.OrganizationId;
        WelcomeLetter = propertyHtml.WelcomeLetter;
        InspectionChecklist = propertyHtml.InspectionChecklist;
        Lease = propertyHtml.Lease;
        Invoice = propertyHtml.Invoice;
        LetterOfResponsibility = propertyHtml.LetterOfResponsibility;
        NoticeToVacate = propertyHtml.NoticeToVacate;
        CreditAuthorization = propertyHtml.CreditAuthorization;
        CreditApplicationBusiness = propertyHtml.CreditApplicationBusiness;
        CreditApplicationIndividual = propertyHtml.CreditApplicationIndividual;
        IsDeleted = propertyHtml.IsDeleted;
        CreatedOn = propertyHtml.CreatedOn;
        CreatedBy = propertyHtml.CreatedBy;
        ModifiedOn = propertyHtml.ModifiedOn;
        ModifiedBy = propertyHtml.ModifiedBy;
    }
}


namespace RentAll.Infrastructure.Entities.Properties;

public class PropertyHtmlEntity
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
}



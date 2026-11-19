namespace RentAll.Infrastructure.Entities.Emails;

public class EmailTemplateEntity
{
    public int EmailTemplateId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string EmailName { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
}

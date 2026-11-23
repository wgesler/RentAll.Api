namespace RentAll.Api.Dtos.Emails.EmailTemplates;

/// <summary>A SendGrid dynamic template ID saved for an email, per organization and optionally per office.</summary>
public class EmailTemplateResponseDto
{
    public int EmailTemplateId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string EmailName { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;

    public EmailTemplateResponseDto(EmailTemplate template)
    {
        EmailTemplateId = template.EmailTemplateId;
        OrganizationId = template.OrganizationId;
        OfficeId = template.OfficeId;
        EmailName = template.EmailName;
        TemplateId = template.TemplateId;
    }
}

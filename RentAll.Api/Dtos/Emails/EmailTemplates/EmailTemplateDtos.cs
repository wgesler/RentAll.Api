namespace RentAll.Api.Dtos.Emails.EmailTemplates;

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

/// <summary>An email name the settings screen can set a template for.</summary>
public class EmailTemplateNameDto
{
    public string EmailName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Create, or replace the template of the existing row for the same organization, office and email.</summary>
public class SaveEmailTemplateDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string EmailName { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!EmailTemplateNames.IsKnown(EmailName))
            return (false, $"Unknown email name: {EmailName}");

        var templateId = (TemplateId ?? string.Empty).Trim();
        if (templateId.Length == 0)
            return (false, "TemplateId is required");
        if (templateId.Length > 100)
            return (false, "TemplateId must be 100 characters or less");
        // SendGrid dynamic template IDs look like d-e552f599e887468a988ad03598d07097.
        if (!templateId.StartsWith("d-", StringComparison.OrdinalIgnoreCase) || templateId.Any(char.IsWhiteSpace))
            return (false, "TemplateId must be a SendGrid dynamic template ID (it starts with d-)");

        return (true, null);
    }
}

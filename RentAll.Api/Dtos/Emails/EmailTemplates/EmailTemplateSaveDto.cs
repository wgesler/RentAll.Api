namespace RentAll.Api.Dtos.Emails.EmailTemplates;

/// <summary>Create, or replace the template of the existing row for the same organization, office and email.</summary>
public class EmailTemplateSaveDto
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

    /// <summary>The template to save; the controller sets the scope (organization/office) and the existing row's id.</summary>
    public EmailTemplate ToModel()
    {
        return new EmailTemplate
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            EmailName = EmailTemplateNames.Canonical(EmailName),
            TemplateId = TemplateId.Trim()
        };
    }
}

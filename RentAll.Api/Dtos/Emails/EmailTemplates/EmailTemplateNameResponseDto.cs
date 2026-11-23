namespace RentAll.Api.Dtos.Emails.EmailTemplates;

/// <summary>An email name the settings screen can set a template for.</summary>
public class EmailTemplateNameResponseDto
{
    public string EmailName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public EmailTemplateNameResponseDto(string emailName, string displayName)
    {
        EmailName = emailName;
        DisplayName = displayName;
    }
}

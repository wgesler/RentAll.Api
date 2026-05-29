namespace RentAll.Api.Dtos.Leads.Owners;

public class PublicOwnerDocumentDownloadDto
{
    public string HtmlContent { get; set; } = string.Empty;
    public string? FileName { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(HtmlContent))
            return (false, "HTML content is required");

        return (true, null);
    }
}

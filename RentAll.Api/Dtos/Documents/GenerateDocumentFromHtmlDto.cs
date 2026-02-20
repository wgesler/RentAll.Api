using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Documents;

public class GenerateDocumentFromHtmlDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ReservationId { get; set; }
    public int DocumentTypeId { get; set; }
    public string HtmlContent { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(HtmlContent))
            return (false, "HTML content is required");

        if (!Enum.IsDefined(typeof(DocumentType), DocumentTypeId))
            return (false, $"Invalid DocumentType value: {DocumentTypeId}");

        return (true, null);
    }
}



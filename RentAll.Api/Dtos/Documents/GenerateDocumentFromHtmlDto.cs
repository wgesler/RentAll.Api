namespace RentAll.Api.Dtos.Documents;

public class GenerateDocumentFromHtmlDto
{
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ReservationId { get; set; }
	public int DocumentTypeId { get; set; }
	public string HtmlContent { get; set; } = string.Empty;
	public string? FileName { get; set; }
}



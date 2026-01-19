using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyHtmls;

public class PropertyHtmlResponseDto
{
	public Guid PropertyId { get; set; }
	public Guid OrganizationId { get; set; }
	public string WelcomeLetter { get; set; } = string.Empty;
	public string Lease { get; set; } = string.Empty;
	public string LetterOfResponsibility { get; set; } = string.Empty;
	public string NoticeToVacate { get; set; } = string.Empty;
	public string CreditAuthorization { get; set; } = string.Empty;
	public string CreditApplication { get; set; } = string.Empty;
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
		Lease = propertyHtml.Lease;
		LetterOfResponsibility = propertyHtml.LetterOfResponsibility;
		NoticeToVacate = propertyHtml.NoticeToVacate;
		CreditAuthorization = propertyHtml.CreditAuthorization;
		CreditApplication = propertyHtml.CreditApplication;
		IsDeleted = propertyHtml.IsDeleted;
		CreatedOn = propertyHtml.CreatedOn;
		CreatedBy = propertyHtml.CreatedBy;
		ModifiedOn = propertyHtml.ModifiedOn;
		ModifiedBy = propertyHtml.ModifiedBy;
	}
}


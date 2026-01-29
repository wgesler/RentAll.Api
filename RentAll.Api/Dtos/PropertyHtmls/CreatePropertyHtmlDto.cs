using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyHtmls;

public class CreatePropertyHtmlDto
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

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (string.IsNullOrWhiteSpace(WelcomeLetter))
			return (false, "WelcomeLetter is required");

		if (string.IsNullOrWhiteSpace(Lease))
			return (false, "Lease is required");

		return (true, null);
	}

	public PropertyHtml ToModel(Guid currentUser)
	{
		return new PropertyHtml
		{
			PropertyId = PropertyId,
			OrganizationId = OrganizationId,
			WelcomeLetter = WelcomeLetter,
			InspectionChecklist = InspectionChecklist,
			Lease = Lease,
			Invoice = Invoice,
			LetterOfResponsibility = LetterOfResponsibility,
			NoticeToVacate = NoticeToVacate,
			CreditAuthorization = CreditAuthorization,
			CreditApplicationBusiness = CreditApplicationBusiness,
			CreditApplicationIndividual = CreditApplicationIndividual,
			IsDeleted = false,
			CreatedBy = currentUser
		};
	}
}


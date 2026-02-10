using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations;

public class OrganizationResponseDto
{
	public Guid OrganizationId { get; set; }
	public string OrganizationCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? Zip { get; set; }
	public string Phone { get; set; } = string.Empty;
	public string? Fax { get; set; }
	public string? Website { get; set; }
	public string? LogoPath { get; set; }
	public FileDetails? FileDetails { get; set; }
	public bool IsInternational { get; set; }
	public bool IsActive { get; set; }


	public OrganizationResponseDto(Organization org)
	{
		OrganizationId = org.OrganizationId;
		OrganizationCode = org.OrganizationCode;
		Name = org.Name;
		Address1 = org.Address1;
		Address2 = org.Address2;
		Suite = org.Suite;
		City = org.City;
		State = org.State;
		Zip = org.Zip;
		Phone = org.Phone;
		Fax = org.Fax;
		Website = org.Website;
		LogoPath = org.LogoPath;
		FileDetails = org.FileDetails;
		IsInternational = org.IsInternational;
		IsActive = org.IsActive;
	}
}





using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Offices;

public class OfficeResponseDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string OfficeCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Fax { get; set; }
	public string? Website { get; set; }
	public string? LogoPath { get; set; }
	public FileDetails? FileDetails { get; set; }
	public bool IsActive { get; set; }

	public OfficeResponseDto(Office office)
	{
		OrganizationId = office.OrganizationId;
		OfficeId = office.OfficeId;
		OfficeCode = office.OfficeCode;
		Name = office.Name;
		Address1 = office.Address1;
		Address2 = office.Address2;
		Suite = office.Suite;
		City = office.City;
		State = office.State;
		Zip = office.Zip;
		Phone = office.Phone;
		Fax = office.Fax;
		Website = office.Website;
		LogoPath = office.LogoPath;
		FileDetails = office.FileDetails;
		IsActive = office.IsActive;
	}
}


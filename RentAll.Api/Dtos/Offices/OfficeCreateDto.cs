using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Offices;

public class OfficeCreateDto
{
	public Guid OrganizationId { get; set; }
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
	public FileDetails? FileDetails { get; set; }
	public bool IsActive { get; set; }

	public Office ToModel()
	{
		return new Office
		{
			OrganizationId = OrganizationId,
			OfficeCode = OfficeCode,
			Name = Name,
			Address1 = Address1,
			Address2 = Address2,
			Suite = Suite,
			City = City,
			State = State,
			Zip = Zip,
			Phone = Phone,
			Fax = Fax,
			Website = Website,
			LogoPath = null, // Will be set by controller after file save
			IsActive = IsActive
		};
	}
}


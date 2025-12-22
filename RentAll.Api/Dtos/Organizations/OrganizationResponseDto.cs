using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Organizations;

public class OrganizationResponseDto
{
	public Guid OrganizationId { get; set; }
	public string OrganizationCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Website { get; set; }
	public string? MaintenanceEmail { get; set; }
	public string? AfterHoursPhone { get; set; }
	public Guid? LogoStorageId { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }


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
		Website = org.Website;
		MaintenanceEmail = org.MaintenanceEmail;
		AfterHoursPhone = org.AfterHoursPhone;
		LogoStorageId = org.LogoStorageId;
		IsActive = org.IsActive;
		CreatedOn = org.CreatedOn;
		CreatedBy = org.CreatedBy;
		ModifiedOn = org.ModifiedOn;
		ModifiedBy = org.ModifiedBy;
	}
}





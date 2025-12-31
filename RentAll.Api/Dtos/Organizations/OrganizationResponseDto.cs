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
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Fax { get; set; }
	public string? Website { get; set; }
	public string? MaintenanceEmail { get; set; }
	public string? AfterHoursPhone { get; set; }
	public decimal DefaultDeposit { get; set; }
	public decimal UtilityOneBed { get; set; }
	public decimal UtilityTwoBed { get; set; }
	public decimal UtilityThreeBed { get; set; }
	public decimal UtilityFourBed { get; set; }
	public decimal UtilityHouse { get; set; }
	public decimal MaidOneBed { get; set; }
	public decimal MaidTwoBed { get; set; }
	public decimal MaidThreeBed { get; set; }
	public decimal MaidFourBed { get; set; }
	public string? LogoPath { get; set; }
	public FileDetails? FileDetails { get; set; }
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
		Fax = org.Fax;
		Website = org.Website;
		MaintenanceEmail = org.MaintenanceEmail;
		AfterHoursPhone = org.AfterHoursPhone;
		DefaultDeposit = org.DefaultDeposit;
		UtilityOneBed = org.UtilityOneBed;
		UtilityTwoBed = org.UtilityTwoBed;
		UtilityThreeBed = org.UtilityThreeBed;
		UtilityFourBed = org.UtilityFourBed;
		UtilityHouse = org.UtilityHouse;
		MaidOneBed = org.MaidOneBed;
		MaidTwoBed = org.MaidTwoBed;
		MaidThreeBed = org.MaidThreeBed;
		MaidFourBed = org.MaidFourBed;
		LogoPath = org.LogoPath;
		IsActive = org.IsActive;
		CreatedOn = org.CreatedOn;
		CreatedBy = org.CreatedBy;
		ModifiedOn = org.ModifiedOn;
		ModifiedBy = org.ModifiedBy;
	}
}





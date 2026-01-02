using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.OfficeConfigurations;

public class OfficeConfigurationResponseDto
{
	public int OfficeId { get; set; }
	public string? MaintenanceEmail { get; set; }
	public string? AfterHoursPhone { get; set; }
	public string? AfterHoursInstructions { get; set; }
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
	public decimal ParkingLowEnd { get; set; }
	public decimal ParkingHighEnd { get; set; }
	public bool IsActive { get; set; }

	public OfficeConfigurationResponseDto(OfficeConfiguration officeConfiguration)
	{
		OfficeId = officeConfiguration.OfficeId;
		MaintenanceEmail = officeConfiguration.MaintenanceEmail;
		AfterHoursPhone = officeConfiguration.AfterHoursPhone;
		AfterHoursInstructions = officeConfiguration.AfterHoursInstructions;
		DefaultDeposit = officeConfiguration.DefaultDeposit;
		UtilityOneBed = officeConfiguration.UtilityOneBed;
		UtilityTwoBed = officeConfiguration.UtilityTwoBed;
		UtilityThreeBed = officeConfiguration.UtilityThreeBed;
		UtilityFourBed = officeConfiguration.UtilityFourBed;
		UtilityHouse = officeConfiguration.UtilityHouse;
		MaidOneBed = officeConfiguration.MaidOneBed;
		MaidTwoBed = officeConfiguration.MaidTwoBed;
		MaidThreeBed = officeConfiguration.MaidThreeBed;
		MaidFourBed = officeConfiguration.MaidFourBed;
		ParkingLowEnd = officeConfiguration.ParkingLowEnd;
		ParkingHighEnd = officeConfiguration.ParkingHighEnd;
		IsActive = officeConfiguration.IsActive;
	}
}


using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.OfficeConfigurations;

public class OfficeConfigurationUpdateDto
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

	public OfficeConfiguration ToModel()
	{
		return new OfficeConfiguration
		{
			OfficeId = OfficeId,
			MaintenanceEmail = MaintenanceEmail,
			AfterHoursPhone = AfterHoursPhone,
			AfterHoursInstructions = AfterHoursInstructions,
			DefaultDeposit = DefaultDeposit,
			UtilityOneBed = UtilityOneBed,
			UtilityTwoBed = UtilityTwoBed,
			UtilityThreeBed = UtilityThreeBed,
			UtilityFourBed = UtilityFourBed,
			UtilityHouse = UtilityHouse,
			MaidOneBed = MaidOneBed,
			MaidTwoBed = MaidTwoBed,
			MaidThreeBed = MaidThreeBed,
			MaidFourBed = MaidFourBed,
			ParkingLowEnd = ParkingLowEnd,
			ParkingHighEnd = ParkingHighEnd,
			IsActive = IsActive
		};
	}
}



using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Properties;

public class PropertyListResponseDto
{
	public Guid PropertyId { get; set; }
	public string PropertyCode { get; set; } = string.Empty;
	public string ShortAddress { get; set; } = string.Empty;
	public int OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public Guid Owner1Id { get; set; }
	public string OwnerName { get; set; } = string.Empty;
	public int Bedrooms { get; set; }
	public decimal Bathrooms { get; set; }
	public int Accomodates { get; set; }
	public int SquareFeet { get; set; }
	public decimal MonthlyRent { get; set; }
	public decimal DailyRate { get; set; }
	public decimal DepartureFee { get; set; }
	public decimal PetFee { get; set; }
	public decimal MaidServiceFee { get; set; }
	public int PropertyStatusId { get; set; }
	public bool IsActive { get; set; }

	public PropertyListResponseDto(PropertyList propertyList)
	{
		PropertyId = propertyList.PropertyId;
		PropertyCode = propertyList.PropertyCode;
		ShortAddress = propertyList.ShortAddress;
		OfficeId = propertyList.OfficeId;
		OfficeName = propertyList.OfficeName;
		Owner1Id = propertyList.Owner1Id;
		OwnerName = propertyList.OwnerName;
		Bedrooms = propertyList.Bedrooms;
		Bathrooms = propertyList.Bathrooms;
		Accomodates = propertyList.Accomodates;
		SquareFeet = propertyList.SquareFeet;
		MonthlyRent = propertyList.MonthlyRent;
		DailyRate = propertyList.DailyRate;
		DepartureFee = propertyList.DepartureFee;
		PetFee = propertyList.PetFee;
		MaidServiceFee = propertyList.MaidServiceFee;
		PropertyStatusId = (int)propertyList.PropertyStatus;
		IsActive = propertyList.IsActive;
	}
}


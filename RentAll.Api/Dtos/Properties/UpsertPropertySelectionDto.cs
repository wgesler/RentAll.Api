using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Properties;

public class UpsertPropertySelectionDto
{
	public Guid UserId { get; set; }
	public int FromBeds { get; set; }
	public int ToBeds { get; set; }
	public int Accomodates { get; set; }
	public decimal MaxRent { get; set; }
	public string? PropertyCode { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public bool Unfurnished { get; set; }
	public bool Cable { get; set; }
	public bool Streaming { get; set; }
	public bool Pool { get; set; }
	public bool Jacuzzi { get; set; }
	public bool Security { get; set; }
	public bool Parking { get; set; }
	public bool Pets { get; set; }
	public bool Smoking { get; set; }
	public bool HighSpeedInternet { get; set; }
	public int PropertyStatusId { get; set; }
	public string? OfficeCode { get; set; }
	public string? BuildingCode { get; set; }
	public string? RegionCode { get; set; }
	public string? AreaCode { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid(Guid currentUser)
	{
		if (UserId == Guid.Empty || UserId != currentUser)
			return (false, "UserId is invalid");

		if (FromBeds < 0)
			return (false, "FromBeds must be zero or greater");

		if (ToBeds < 0)
			return (false, "ToBeds must be zero or greater");

		if (ToBeds < FromBeds)
			return (false, "ToBeds must be greater than or equal to FromBeds");

		if (Accomodates < 0)
			return (false, "Accomodates must be zero or greater");

		if (MaxRent < 0)
			return (false, "MaxRent must be zero or greater");

		return (true, null);
	}

	public PropertySelection ToModel()
	{
		return new PropertySelection
		{
			UserId = UserId,
			FromBeds = FromBeds,
			ToBeds = ToBeds,
			Accomodates = Accomodates,
			MaxRent = MaxRent,
			PropertyCode = PropertyCode,
			City = City,
			State = State,
			Unfurnished = Unfurnished,
			Cable = Cable,
			Streaming = Streaming,
			Pool = Pool,
			Jacuzzi = Jacuzzi,
			Security = Security,
			Parking = Parking,
			Pets = Pets,
			Smoking = Smoking,
			HighSpeedInternet = HighSpeedInternet,
			PropertyStatusId = PropertyStatusId,
			OfficeCode = OfficeCode,
			BuildingCode = BuildingCode,
			RegionCode = RegionCode,
			AreaCode = AreaCode
		};
	}
}



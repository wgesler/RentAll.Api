namespace RentAll.Infrastructure.Entities;

public class PropertySelectionEntity
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
}



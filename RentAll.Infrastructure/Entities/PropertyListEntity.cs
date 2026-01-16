namespace RentAll.Infrastructure.Entities;

public class PropertyListEntity
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
}

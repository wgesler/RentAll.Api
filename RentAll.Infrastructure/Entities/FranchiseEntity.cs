namespace RentAll.Infrastructure.Entities;

public class FranchiseEntity
{
	public int FranchiseId { get; set; }
	public Guid OrganizationId { get; set; }
	public string FranchiseCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}


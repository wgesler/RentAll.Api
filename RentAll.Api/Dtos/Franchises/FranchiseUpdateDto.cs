using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Franchises;

public class FranchiseUpdateDto
{
	public Guid OrganizationId { get; set; }
	public int FranchiseId { get; set; }
	public string FranchiseCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Franchise ToModel()
	{
		return new Franchise
		{
			OrganizationId = OrganizationId,
			FranchiseId = FranchiseId,
			FranchiseCode = FranchiseCode,
			Description = Description,
			Phone = Phone,
			IsActive = IsActive
		};
	}
}


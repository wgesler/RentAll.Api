using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Franchises;

public class FranchiseCreateDto
{
	public Guid OrganizationId { get; set; }
	public string FranchiseCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Franchise ToModel()
	{
		return new Franchise
		{
			OrganizationId = OrganizationId,
			FranchiseCode = FranchiseCode,
			Description = Description,
			IsActive = IsActive
		};
	}
}

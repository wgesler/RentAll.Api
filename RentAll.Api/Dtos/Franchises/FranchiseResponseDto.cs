using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Franchises;

public class FranchiseResponseDto
{
	public Guid OrganizationId { get; set; }
	public int FranchiseId { get; set; }
	public string FranchiseCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public FranchiseResponseDto(Franchise franchise)
	{
		OrganizationId = franchise.OrganizationId;
		FranchiseId = franchise.FranchiseId;
		FranchiseCode = franchise.FranchiseCode;
		Description = franchise.Description;
		IsActive = franchise.IsActive;
	}
}
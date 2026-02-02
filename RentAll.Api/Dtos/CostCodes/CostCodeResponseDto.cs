using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.CostCodes;

public class CostCodeResponseDto
{
	public int CostCodeId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string CostCode { get; set; } = string.Empty;
	public int TransactionTypeId { get; set; }
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public CostCodeResponseDto(CostCode costCode)
	{
		CostCodeId = costCode.CostCodeId;
		OrganizationId = costCode.OrganizationId;
		OfficeId = costCode.OfficeId;
		CostCode = costCode.Code;
		TransactionTypeId = (int)costCode.TransactionType;
		Description = costCode.Description;
		IsActive = costCode.IsActive;
	}
}

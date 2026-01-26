using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class CostCode
{
	public int CostCodeId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string Code { get; set; } = string.Empty;
	public TransactionType TransactionType { get; set; }
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}

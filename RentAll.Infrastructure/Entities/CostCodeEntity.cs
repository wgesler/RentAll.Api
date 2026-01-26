namespace RentAll.Infrastructure.Entities;

public class CostCodeEntity
{
	public int CostCodeId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string CostCode{ get; set; } = string.Empty;
	public int TransactionTypeId { get; set; }
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}

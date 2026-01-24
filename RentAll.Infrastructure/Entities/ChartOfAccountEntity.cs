namespace RentAll.Infrastructure.Entities;

public class ChartOfAccountEntity
{
	public Guid ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public int AccountId{ get; set; } 
	public string Description { get; set; } = string.Empty;
	public int AccountTypeId { get; set; }
}

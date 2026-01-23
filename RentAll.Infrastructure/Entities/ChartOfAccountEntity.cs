namespace RentAll.Infrastructure.Entities;

public class ChartOfAccountEntity
{
	public int ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public int AccountTypeId { get; set; }
}

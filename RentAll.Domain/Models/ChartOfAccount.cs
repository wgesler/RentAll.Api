using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ChartOfAccount
{
	public int ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public AccountType AccountType { get; set; }
}

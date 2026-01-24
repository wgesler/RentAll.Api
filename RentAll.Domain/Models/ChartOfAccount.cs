using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ChartOfAccount
{
	public Guid ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public int AccountId { get; set; }
	public string Description { get; set; } = string.Empty;
	public AccountType AccountType { get; set; }
	public bool IsActive { get; set; }
}

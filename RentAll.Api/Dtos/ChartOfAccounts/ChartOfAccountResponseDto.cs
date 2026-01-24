using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.ChartOfAccounts;

public class ChartOfAccountResponseDto
{
	public Guid ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public int AccountId { get; set; }
	public string Description { get; set; } = string.Empty;
	public AccountType AccountType { get; set; }
	public bool IsActive { get; set; }

	public ChartOfAccountResponseDto(ChartOfAccount chartOfAccount)
	{
		ChartOfAccountId = chartOfAccount.ChartOfAccountId;
		OrganizationId = chartOfAccount.OrganizationId;
		OfficeId = chartOfAccount.OfficeId;
		AccountId = chartOfAccount.AccountId;
		Description = chartOfAccount.Description;
		AccountType = chartOfAccount.AccountType;
		IsActive = chartOfAccount.IsActive;
	}
}

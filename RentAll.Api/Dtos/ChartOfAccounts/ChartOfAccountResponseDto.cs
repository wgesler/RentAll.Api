using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.ChartOfAccounts;

public class ChartOfAccountResponseDto
{
	public int ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public AccountType AccountType { get; set; }

	public ChartOfAccountResponseDto(ChartOfAccount chartOfAccount)
	{
		ChartOfAccountId = chartOfAccount.ChartOfAccountId;
		OrganizationId = chartOfAccount.OrganizationId;
		AccountNumber = chartOfAccount.AccountNumber;
		Description = chartOfAccount.Description;
		AccountType = chartOfAccount.AccountType;
	}
}

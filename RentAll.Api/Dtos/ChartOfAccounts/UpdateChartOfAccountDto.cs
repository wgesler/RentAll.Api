using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.ChartOfAccounts;

public class UpdateChartOfAccountDto
{
	public int ChartOfAccountId { get; set; }
	public Guid OrganizationId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public AccountType AccountType { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(int id)
	{
		if (id == 0)
			return (false, "ChartOfAccount ID is required");

		if (ChartOfAccountId != id)
			return (false, "ChartOfAccount ID mismatch");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (string.IsNullOrWhiteSpace(AccountNumber))
			return (false, "AccountNumber is required");

		if (string.IsNullOrWhiteSpace(Description))
			return (false, "Description is required");

		return (true, null);
	}

	public ChartOfAccount ToModel()
	{
		return new ChartOfAccount
		{
			ChartOfAccountId = ChartOfAccountId,
			OrganizationId = OrganizationId,
			AccountNumber = AccountNumber,
			Description = Description,
			AccountType = AccountType
		};
	}
}

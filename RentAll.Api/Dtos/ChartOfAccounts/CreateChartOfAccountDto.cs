using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.ChartOfAccounts;

public class CreateChartOfAccountDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public AccountType AccountType { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (OfficeId <= 0)
			return (false, "OfficeId is required");

		if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
			return (false, "Unauthorized");

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
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			AccountNumber = AccountNumber,
			Description = Description,
			AccountType = AccountType
		};
	}
}

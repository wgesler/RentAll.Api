using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.AccountingOffices;

public partial class AccountingOfficeRepository : IAccountingOfficeRepository
{
	public async Task<IEnumerable<AccountingOffice>> GetAllByOfficeIdAsync(Guid organizationId, string officeIds)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Accounting.Office_GetAllByOfficeIds", new
		{
			OrganizationId = organizationId,
			Offices = officeIds
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<AccountingOffice>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<AccountingOffice?> GetByIdAsync(Guid organizationId, int officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Accounting.Office_GetById", new
		{
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

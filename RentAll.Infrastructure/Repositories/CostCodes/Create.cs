using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CostCodes;

public partial class CostCodeRepository : ICostCodeRepository
{
	public async Task<CostCode> CreateAsync(CostCode costCode)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_Add", new
		{
			OrganizationId = costCode.OrganizationId,
			OfficeId = costCode.OfficeId,
			CostCode = costCode.Code,
			TransactionTypeId = (int)costCode.TransactionType,
			Description = costCode.Description,
			IsActive = costCode.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("CostCode not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

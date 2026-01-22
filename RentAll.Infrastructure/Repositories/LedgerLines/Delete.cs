using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.LedgerLines;

public partial class LedgerLineRepository : ILedgerLineRepository
{
	public async Task DeleteByIdAsync(int ledgerLineId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.LedgerLine_DeleteById", new
		{
			LedgerLineId = ledgerLineId
		});
	}
}

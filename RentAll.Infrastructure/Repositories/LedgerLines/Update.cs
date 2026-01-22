using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LedgerLines;

public partial class LedgerLineRepository : ILedgerLineRepository
{
	public async Task<LedgerLine> UpdateByIdAsync(LedgerLine ledgerLine)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_UpdateById", new
		{
			LedgerLineId = ledgerLine.LedgerLineId,
			ChartOfAccountId = ledgerLine.ChartOfAccountId,
			TransactionTypeId = (int)ledgerLine.TransactionType,
			InvoiceId = ledgerLine.InvoiceId,
			PropertyId = ledgerLine.PropertyId,
			ReservationId = ledgerLine.ReservationId,
			Amount = ledgerLine.Amount
		});

		if (res == null || !res.Any())
			throw new Exception("LedgerLine not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

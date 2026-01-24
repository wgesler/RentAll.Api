using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LedgerLines;

public partial class LedgerLineRepository : ILedgerLineRepository
{
	public async Task<LedgerLine> CreateAsync(LedgerLine ledgerLine)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_Add", new
		{
			ChartOfAccountId = ledgerLine.ChartOfAccountId,
			TransactionTypeId = (int)ledgerLine.TransactionType,
			InvoiceId = ledgerLine.InvoiceId,
			PropertyId = ledgerLine.PropertyId,
			ReservationId = ledgerLine.ReservationId,
			Amount = ledgerLine.Amount,
			Description = ledgerLine.Description
		});

		if (res == null || !res.Any())
			throw new Exception("LedgerLine not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.InvoiceLedgerLines;

public partial class InvoiceLedgerLineRepository : IInvoiceLedgerLineRepository
{
	public async Task<IEnumerable<InvoiceLedgerLine>> GetByInvoiceIdAsync(Guid invoiceId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceLedgerLineEntity>("Accounting.InvoiceLedgerLine_GetByInvoiceId", new
		{
			InvoiceId = invoiceId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<InvoiceLedgerLine>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<InvoiceLedgerLine>> GetByLedgerLineIdAsync(int ledgerLineId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceLedgerLineEntity>("Accounting.InvoiceLedgerLine_GetByLedgerLineId", new
		{
			LedgerLineId = ledgerLineId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<InvoiceLedgerLine>();

		return res.Select(ConvertEntityToModel);
	}
}

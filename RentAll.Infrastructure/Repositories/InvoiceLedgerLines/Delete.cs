using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.InvoiceLedgerLines;

public partial class InvoiceLedgerLineRepository : IInvoiceLedgerLineRepository
{
	public async Task DeleteAsync(Guid invoiceId, int ledgerLineId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.InvoiceLedgerLine_Delete", new
		{
			InvoiceId = invoiceId,
			LedgerLineId = ledgerLineId
		});
	}

	public async Task DeleteByInvoiceIdAsync(Guid invoiceId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.InvoiceLedgerLine_DeleteByInvoiceId", new
		{
			InvoiceId = invoiceId
		});
	}

	public async Task DeleteByLedgerLineIdAsync(int ledgerLineId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.InvoiceLedgerLine_DeleteByLedgerLineId", new
		{
			LedgerLineId = ledgerLineId
		});
	}
}

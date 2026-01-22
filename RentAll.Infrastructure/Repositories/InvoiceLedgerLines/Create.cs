using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.InvoiceLedgerLines;

public partial class InvoiceLedgerLineRepository : IInvoiceLedgerLineRepository
{
	public async Task<InvoiceLedgerLine> CreateAsync(InvoiceLedgerLine invoiceLedgerLine)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceLedgerLineEntity>("Accounting.InvoiceLedgerLine_Add", new
		{
			InvoiceId = invoiceLedgerLine.InvoiceId,
			LedgerLineId = invoiceLedgerLine.LedgerLineId
		});

		if (res == null || !res.Any())
			throw new Exception("InvoiceLedgerLine not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

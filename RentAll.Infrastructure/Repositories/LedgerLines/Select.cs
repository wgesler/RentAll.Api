using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LedgerLines;

public partial class LedgerLineRepository : ILedgerLineRepository
{
	public async Task<LedgerLine?> GetByIdAsync(int ledgerLineId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_GetById", new
		{
			LedgerLineId = ledgerLineId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<IEnumerable<LedgerLine>> GetByInvoiceIdAsync(Guid invoiceId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_GetByInvoiceId", new
		{
			InvoiceId = invoiceId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<LedgerLine>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<LedgerLine>> GetByPropertyIdAsync(Guid propertyId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_GetByPropertyId", new
		{
			PropertyId = propertyId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<LedgerLine>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<LedgerLine>> GetByReservationIdAsync(Guid reservationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_GetByReservationId", new
		{
			ReservationId = reservationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<LedgerLine>();

		return res.Select(ConvertEntityToModel);
	}
}

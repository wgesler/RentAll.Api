using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LedgerLines;

public partial class LedgerLineRepository : ILedgerLineRepository
{
	private readonly string _dbConnectionString;

	public LedgerLineRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private LedgerLine ConvertEntityToModel(LedgerLineEntity e)
	{
		return new LedgerLine
		{
			LedgerLineId = e.LedgerLineId,
			ChartOfAccountId = e.ChartOfAccountId,
			TransactionType = (TransactionType)e.TransactionTypeId,
			InvoiceId = e.InvoiceId,
			PropertyId = e.PropertyId,
			ReservationId = e.ReservationId,
			Amount = e.Amount
		};
	}
}

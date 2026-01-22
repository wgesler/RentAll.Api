using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.InvoiceLedgerLines;

public partial class InvoiceLedgerLineRepository : IInvoiceLedgerLineRepository
{
	private readonly string _dbConnectionString;

	public InvoiceLedgerLineRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private InvoiceLedgerLine ConvertEntityToModel(InvoiceLedgerLineEntity e)
	{
		return new InvoiceLedgerLine
		{
			InvoiceId = e.InvoiceId,
			LedgerLineId = e.LedgerLineId
		};
	}
}

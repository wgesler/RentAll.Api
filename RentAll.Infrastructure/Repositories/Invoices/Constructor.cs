using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Invoices;

public partial class InvoiceRepository : IInvoiceRepository
{
	private readonly string _dbConnectionString;

	public InvoiceRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Invoice ConvertEntityToModel(InvoiceEntity e)
	{
		return new Invoice
		{
			InvoiceId = e.InvoiceId,
			OrganizationId = e.OrganizationId,
			OfficeId = e.OfficeId,
			OfficeName = e.OfficeName,
			ReservationId = e.ReservationId,
			ReservationCode = e.ReservationCode,
			ContactId = e.ContactId,
			ContactName = e.ContactName,
			InvoiceDate = e.InvoiceDate,
			DueDate = e.DueDate,
			TotalAmount = e.TotalAmount,
			PaidAmount = e.PaidAmount,
			Notes = e.Notes,
			IsActive = e.IsActive,
			CreatedOn = e.CreatedOn,
			CreatedBy = e.CreatedBy,
			ModifiedOn = e.ModifiedOn,
			ModifiedBy = e.ModifiedBy
		};
	}
}

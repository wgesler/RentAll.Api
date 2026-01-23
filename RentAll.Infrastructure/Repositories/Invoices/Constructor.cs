using System.Text.Json;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Invoices;

public partial class InvoiceRepository : IInvoiceRepository
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly string _dbConnectionString;

	public InvoiceRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Invoice ConvertEntityToModel(InvoiceEntity e)
	{
		List<LedgerLine> lines = new List<LedgerLine>();
		if (!string.IsNullOrWhiteSpace(e.Lines))
		{
			try
			{
				lines = JsonSerializer.Deserialize<List<LedgerLine>>(e.Lines, JsonOptions) ?? new List<LedgerLine>();
			}
			catch
			{
				lines = new List<LedgerLine>();
			}
		}

		return new Invoice
		{
			InvoiceId = e.InvoiceId,
			OrganizationId = e.OrganizationId,
			OfficeId = e.OfficeId,
			OfficeName = e.OfficeName,
			InvoiceName = e.InvoiceName,
			ReservationId = e.ReservationId,
			ReservationCode = e.ReservationCode,
			InvoiceDate = e.InvoiceDate,
			DueDate = e.DueDate,
			TotalAmount = e.TotalAmount,
			PaidAmount = e.PaidAmount,
			Notes = e.Notes,
			Lines = lines
		};
	}
}

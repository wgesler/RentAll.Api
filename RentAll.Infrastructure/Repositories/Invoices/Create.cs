using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Text.Json;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Invoices;

public partial class InvoiceRepository : IInvoiceRepository
{
	public async Task<Invoice> CreateAsync(Invoice invoice)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var response = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_Add", new
		{
			OrganizationId = invoice.OrganizationId,
			OfficeId = invoice.OfficeId,
			OfficeName = invoice.OfficeName,
			InvoiceCode = invoice.InvoiceCode,
			ReservationId = invoice.ReservationId,
			ReservationCode = invoice.ReservationCode,
			InvoiceDate = invoice.InvoiceDate,
			DueDate = invoice.DueDate,
			TotalAmount = invoice.TotalAmount,
			PaidAmount = invoice.PaidAmount,
			Notes = invoice.Notes,
			IsActive = invoice.IsActive,
			CreatedBy = invoice.CreatedBy
		});

		if (response == null || !response.Any())
			throw new Exception("Invoice not created");

		var i = ConvertEntityToModel(response.FirstOrDefault()!);
		foreach (var line in invoice.LedgerLines)
		{
			var ll = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_Add", new
			{
				InvoiceId = i.InvoiceId,
				ReservationId = line.ReservationId,
				CostCodeId = line.CostCodeId,
				Amount = line.Amount,
				Description = line.Description,
				CreatedBy = invoice.CreatedBy
			});
		}

		// Get fully populated invoice
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetById", new
		{
			InvoiceId = i.InvoiceId,
			OrganizationId = i.OrganizationId
		});

		if (res == null || !res.Any())
			throw new Exception("Invoice not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

using System.Data.SqlClient;
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
		var linesJson = invoice.LedgerLines != null && invoice.LedgerLines.Any() 
			? JsonSerializer.Serialize(invoice.LedgerLines, JsonOptions) 
			: null;

		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_Add", new
		{
			OrganizationId = invoice.OrganizationId,
			OfficeId = invoice.OfficeId,
			OfficeName = invoice.OfficeName,
			InvoiceName = invoice.InvoiceName,
			ReservationId = invoice.ReservationId,
			ReservationCode = invoice.ReservationCode,
			InvoiceDate = invoice.InvoiceDate,
			DueDate = invoice.DueDate,
			TotalAmount = invoice.TotalAmount,
			PaidAmount = invoice.PaidAmount,
			Notes = invoice.Notes,
			IsActive = invoice.IsActive,
			LedgerLines = linesJson
		});

		if (res == null || !res.Any())
			throw new Exception("Invoice not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

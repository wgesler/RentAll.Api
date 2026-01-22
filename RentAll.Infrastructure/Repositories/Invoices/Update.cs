using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Invoices;

public partial class InvoiceRepository : IInvoiceRepository
{
	public async Task<Invoice> UpdateByIdAsync(Invoice invoice)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_UpdateById", new
		{
			InvoiceId = invoice.InvoiceId,
			OrganizationId = invoice.OrganizationId,
			OfficeId = invoice.OfficeId,
			ReservationId = invoice.ReservationId,
			ContactId = invoice.ContactId,
			InvoiceDate = invoice.InvoiceDate,
			DueDate = invoice.DueDate,
			TotalAmount = invoice.TotalAmount,
			PaidAmount = invoice.PaidAmount,
			Notes = invoice.Notes,
			IsActive = invoice.IsActive,
			ModifiedBy = invoice.ModifiedBy
		});

		if (res == null || !res.Any())
			throw new Exception("Invoice not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

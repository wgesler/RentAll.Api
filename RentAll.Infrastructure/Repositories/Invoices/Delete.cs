using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Invoices;

public partial class InvoiceRepository : IInvoiceRepository
{
	public async Task DeleteByIdAsync(Guid invoiceId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.Invoice_DeleteById", new
		{
			InvoiceId = invoiceId,
			OrganizationId = organizationId
		});
	}
}

using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Invoices;

public partial class InvoiceRepository : IInvoiceRepository
{
	public async Task<IEnumerable<Invoice>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Invoice>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<Invoice>> GetAllByOfficeIdsAsync(Guid organizationId, string officeAccess)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAllByOfficeIds", new
		{
			OrganizationId = organizationId,
			Offices = officeAccess
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Invoice>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<Invoice>> GetAllByReservationIdAsync(Guid reservationId, Guid organizationId, string officeAccess)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAllByReservationId", new
		{
			ReservationId = reservationId,
			OrganizationId = organizationId,
			Offices = officeAccess
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Invoice>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<Invoice>> GetAllByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAllByPropertyId", new
		{
			PropertyId = propertyId,
			OrganizationId = organizationId,
			Offices = officeAccess
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Invoice>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<Invoice>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAllByOfficeIds", new
		{
			OrganizationId = organizationId,
			Offices = officeAccess
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Invoice>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Invoice?> GetByIdAsync(Guid invoiceId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetById", new
		{
			InvoiceId = invoiceId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<IEnumerable<Invoice>> GetByReservationIdAsync(Guid reservationId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetByReservationId", new
		{
			ReservationId = reservationId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Invoice>();

		return res.Select(ConvertEntityToModel);
	}
}

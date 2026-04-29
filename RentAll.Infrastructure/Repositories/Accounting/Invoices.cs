using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<IEnumerable<Invoice>> GetInvoicesByOfficeIdsAsync(Guid organizationId, string officeAccess)
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

    public async Task<IEnumerable<Invoice>> GetInvoicesByReservationIdAsync(Guid reservationId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAllByReservationAndOfficeIds", new
        {
            ReservationId = reservationId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Invoice>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetAllByPropertyAndOfficeIds", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Invoice>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid organizationId)
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

    public async Task<Invoice?> GetInvoiceByCodeAsync(string invoiceCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetByCode", new
        {
            InvoiceCode = invoiceCode,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
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
                InvoicePeriod = invoice.InvoicePeriod,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                Notes = invoice.Notes,
                IsActive = invoice.IsActive,
                CreatedBy = invoice.CreatedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Invoice not created");

            var i = ConvertEntityToModel(response.FirstOrDefault()!);
            foreach (var line in invoice.LedgerLines)
            {
                var ll = await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_Add", new
                {
                    InvoiceId = i.InvoiceId,
                    LineNumber = line.LineNumber,
                    ReservationId = line.ReservationId,
                    CostCodeId = line.CostCodeId,
                    Amount = line.Amount,
                    Description = line.Description,
                    CreatedBy = invoice.CreatedBy
                }, transaction: transaction);
            }

            // Get fully populated invoice
            var res = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetById", new
            {
                InvoiceId = i.InvoiceId,
                OrganizationId = i.OrganizationId
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new Exception("Invoice not found");

            await transaction.CommitAsync();
            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    #endregion

    #region Updates
    public async Task<Invoice> UpdateByIdAsync(Invoice invoice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            // Get current invoice with LedgerLines
            var currentInvoiceResult = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetById", new
            {
                InvoiceId = invoice.InvoiceId,
                OrganizationId = invoice.OrganizationId
            }, transaction: transaction);

            if (currentInvoiceResult == null || !currentInvoiceResult.Any())
                throw new Exception("Invoice not found");

            var currentInvoice = ConvertEntityToModel(currentInvoiceResult.FirstOrDefault()!);
            var currentLedgerLineIds = currentInvoice.LedgerLines.Select(ll => ll.LedgerLineId).ToHashSet();
            // Zero-amount lines from update requests are treated as "removed" rows (e.g. UI x-out behavior).
            var incomingActiveLedgerLines = invoice.LedgerLines.Where(ll => ll.Amount != 0).ToList();
            var incomingLedgerLineIds = incomingActiveLedgerLines .Where(ll => ll.LedgerLineId != Guid.Empty) .Select(ll => ll.LedgerLineId) .ToHashSet();

            // Update the Invoice
            var response = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_UpdateById", new
            {
                InvoiceId = invoice.InvoiceId,
                OrganizationId = invoice.OrganizationId,
                OfficeId = invoice.OfficeId,
                OfficeName = invoice.OfficeName,
                InvoiceCode = invoice.InvoiceCode,
                ReservationId = invoice.ReservationId,
                ReservationCode = invoice.ReservationCode,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                InvoicePeriod = invoice.InvoicePeriod,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                Notes = invoice.Notes,
                IsActive = invoice.IsActive,
                ModifiedBy = invoice.ModifiedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Invoice not updated");

            // Sync LedgerLines
            foreach (var line in incomingActiveLedgerLines)
            {
                if (line.LedgerLineId == Guid.Empty)
                {
                    // Create new LedgerLine
                    await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_Add", new
                    {
                        InvoiceId = invoice.InvoiceId,
                        LineNumber = line.LineNumber,
                        ReservationId = line.ReservationId,
                        CostCodeId = line.CostCodeId,
                        Amount = line.Amount,
                        Description = line.Description,
                        CreatedBy = invoice.CreatedBy
                    }, transaction: transaction);
                }
                else if (currentLedgerLineIds.Contains(line.LedgerLineId))
                {
                    // Update existing LedgerLine
                    await db.DapperProcQueryAsync<LedgerLineEntity>("Accounting.LedgerLine_UpdateById", new
                    {
                        LedgerLineId = line.LedgerLineId,
                        InvoiceId = invoice.InvoiceId,
                        LineNumber = line.LineNumber,
                        ReservationId = line.ReservationId,
                        CostCodeId = line.CostCodeId,
                        Amount = line.Amount,
                        Description = line.Description,
                        ModifiedBy = invoice.ModifiedBy
                    }, transaction: transaction);
                }
            }

            // Delete LedgerLines that are no longer in the incoming list
            var ledgerLinesToDelete = currentLedgerLineIds.Except(incomingLedgerLineIds).ToList();
            foreach (var ledgerLineId in ledgerLinesToDelete)
            {
                await db.DapperProcExecuteAsync("Accounting.LedgerLine_DeleteById", new
                {
                    LedgerLineId = ledgerLineId
                }, transaction: transaction);
            }

            // Get fully populated invoice
            var updatedInvoiceResult = await db.DapperProcQueryAsync<InvoiceEntity>("Accounting.Invoice_GetById", new
            {
                InvoiceId = invoice.InvoiceId,
                OrganizationId = invoice.OrganizationId
            }, transaction: transaction);

            if (updatedInvoiceResult == null || !updatedInvoiceResult.Any())
                throw new Exception("Invoice not updated");

            await transaction.CommitAsync();
            return ConvertEntityToModel(updatedInvoiceResult.FirstOrDefault()!);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    #endregion

    #region Deletes
    public async Task DeleteInvoiceByIdAsync(Guid invoiceId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Invoice_DeleteById", new
        {
            InvoiceId = invoiceId,
            OrganizationId = organizationId
        });
    }
    #endregion
}

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<IEnumerable<Invoice>> GetInvoicesAsync(InvoiceGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<InvoiceEntity, LedgerLineEntity>("Accounting.Invoice_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            ReservationId = criteria.ReservationId,
            PropertyId = criteria.PropertyId,
            InvoiceCode = criteria.InvoiceCode,
            IsActive = criteria.IsActive,
            IncludeInactive = criteria.IncludeInactive,
            IncludePaid = criteria.IncludePaid,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        return MapInvoicesWithLedgerLineEntities(headers, lines);
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await LoadInvoiceByIdAsync(db, null, invoiceId, organizationId);
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
                AccountingPeriod = invoice.AccountingPeriod,
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
                    LedgerLineDate = line.LedgerLineDate,
                    CreatedBy = invoice.CreatedBy
                }, transaction: transaction);
            }

            var reloaded = await LoadInvoiceByIdAsync(db, transaction, i.InvoiceId, i.OrganizationId);
            if (reloaded == null)
                throw new Exception("Invoice not found");

            await transaction.CommitAsync();
            return reloaded;
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
            var updatedInvoice = await UpdateByIdCoreAsync(db, transaction, invoice);
            await transaction.CommitAsync();
            return updatedInvoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateInvoiceJournalEntryIdAsync(Invoice invoice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Invoice_UpdateJournalEntryId", new
        {
            InvoiceId = invoice.InvoiceId,
            OrganizationId = invoice.OrganizationId,
            JournalEntryId = invoice.JournalEntryId,
            ModifiedBy = invoice.ModifiedBy
        });
    }

    public async Task<IReadOnlyList<Invoice>> UpdateByIdsInTransactionAsync(IReadOnlyList<Invoice> invoices)
    {
        if (invoices.Count == 0)
            return invoices;

        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var updatedInvoices = new List<Invoice>(invoices.Count);
            foreach (var invoice in invoices)
                updatedInvoices.Add(await UpdateByIdCoreAsync(db, transaction, invoice));

            await transaction.CommitAsync();
            return updatedInvoices;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Invoice> UpdateByIdCoreAsync(SqlConnection db, IDbTransaction transaction, Invoice invoice)
    {
        var currentInvoice = await LoadInvoiceByIdAsync(db, transaction, invoice.InvoiceId, invoice.OrganizationId);
        if (currentInvoice == null)
            throw new Exception("Invoice not found");

        var currentLedgerLineIds = currentInvoice.LedgerLines.Select(ll => ll.LedgerLineId).ToHashSet();
        var currentLedgerLineByLineNumber = currentInvoice.LedgerLines
            .GroupBy(ll => ll.LineNumber)
            .ToDictionary(g => g.Key, g => g.First());
        // Zero-amount lines from update requests are treated as "removed" rows (e.g. UI x-out behavior).
        var incomingActiveLedgerLines = invoice.LedgerLines.Where(ll => ll.Amount != 0).ToList();
        var duplicateIncomingLineNumbers = incomingActiveLedgerLines
            .GroupBy(ll => ll.LineNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(lineNumber => lineNumber)
            .ToList();
        if (duplicateIncomingLineNumbers.Count > 0)
            _logger.LogWarning("Invoice {InvoiceId} update payload contains duplicate ledger line numbers ({LineNumbers}). Using the last row per line number.", invoice.InvoiceId, string.Join(", ", duplicateIncomingLineNumbers));

        // Normalize duplicate line numbers from the UI payload so the final server state
        // is one row per line number (last row wins).
        var dedupedIncomingLedgerLines = incomingActiveLedgerLines
            .GroupBy(ll => ll.LineNumber)
            .Select(g => g.Last())
            .ToList();

        var normalizedIncomingLedgerLines = new List<LedgerLine>(dedupedIncomingLedgerLines.Count);
        var incomingLedgerLineIds = new HashSet<Guid>();
        foreach (var line in dedupedIncomingLedgerLines)
        {
            if (line.LedgerLineId != Guid.Empty && currentLedgerLineIds.Contains(line.LedgerLineId))
            {
                normalizedIncomingLedgerLines.Add(line);
                incomingLedgerLineIds.Add(line.LedgerLineId);
                continue;
            }

            // If the client row id is missing/stale but the line number exists on this invoice,
            // treat it as an update of that existing line instead of an insert.
            if (currentLedgerLineByLineNumber.TryGetValue(line.LineNumber, out var existingLineForNumber)
                && !incomingLedgerLineIds.Contains(existingLineForNumber.LedgerLineId))
            {
                line.LedgerLineId = existingLineForNumber.LedgerLineId;
                normalizedIncomingLedgerLines.Add(line);
                incomingLedgerLineIds.Add(existingLineForNumber.LedgerLineId);
                continue;
            }

            // No matching existing row for this line number: keep as insert candidate.
            line.LedgerLineId = Guid.Empty;
            normalizedIncomingLedgerLines.Add(line);
        }

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
            AccountingPeriod = invoice.AccountingPeriod,
            InvoicePeriod = invoice.InvoicePeriod,
            JournalEntryId = invoice.JournalEntryId,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            Notes = invoice.Notes,
            IsActive = invoice.IsActive,
            ModifiedBy = invoice.ModifiedBy
        }, transaction: transaction);

        if (response == null || !response.Any())
            throw new Exception("Invoice not updated");

        // Delete LedgerLines that are no longer in the incoming list
        var ledgerLinesToDelete = currentLedgerLineIds.Except(incomingLedgerLineIds).ToList();
        foreach (var ledgerLineId in ledgerLinesToDelete)
        {
            await db.DapperProcExecuteAsync("Accounting.LedgerLine_DeleteById", new
            {
                LedgerLineId = ledgerLineId
            }, transaction: transaction);
        }

        // Sync LedgerLines after deletes so line-number uniqueness cannot collide
        // with rows being replaced from UI payloads that use Guid.Empty for new items.
        foreach (var line in normalizedIncomingLedgerLines)
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
                    LedgerLineDate = line.LedgerLineDate,
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
                    LedgerLineDate = line.LedgerLineDate,
                    ModifiedBy = invoice.ModifiedBy
                }, transaction: transaction);
            }
        }

        var updatedInvoice = await LoadInvoiceByIdAsync(db, transaction, invoice.InvoiceId, invoice.OrganizationId);
        if (updatedInvoice == null)
            throw new Exception("Invoice not updated");

        return updatedInvoice;
    }

    private async Task<Invoice?> LoadInvoiceByIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid invoiceId,
        Guid organizationId)
    {
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<InvoiceEntity, LedgerLineEntity>("Accounting.Invoice_GetById", new
        {
            InvoiceId = invoiceId,
            OrganizationId = organizationId
        }, transaction: transaction);

        return MapInvoicesWithLedgerLineEntities(headers, lines).FirstOrDefault();
    }

    private List<Invoice> MapInvoicesWithLedgerLineEntities(
        IEnumerable<InvoiceEntity>? invoiceEntities,
        IEnumerable<LedgerLineEntity>? lineEntities)
    {
        if (invoiceEntities == null || !invoiceEntities.Any())
            return new List<Invoice>();

        var linesByInvoiceId = (lineEntities ?? Enumerable.Empty<LedgerLineEntity>())
            .GroupBy(line => line.InvoiceId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertLedgerLineEntityToModel)
                    .GroupBy(line => line.LedgerLineId)
                    .Select(lineGroup => lineGroup.First())
                    .OrderBy(line => line.LineNumber)
                    .ToList());

        var invoices = invoiceEntities.Select(ConvertEntityToModel).ToList();
        foreach (var invoice in invoices)
        {
            if (linesByInvoiceId.TryGetValue(invoice.InvoiceId, out var lines) && lines.Count > 0)
                invoice.LedgerLines = lines;
        }

        return invoices;
    }
    public async Task<int> DeactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<int>("Accounting.Invoice_DeactivateByReservationId", new
        {
            OrganizationId = organizationId,
            ReservationId = reservationId,
            ModifiedBy = modifiedBy
        });

        return res?.FirstOrDefault() ?? 0;
    }

    public async Task<int> ReactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<int>("Accounting.Invoice_ReactivateByReservationId", new
        {
            OrganizationId = organizationId,
            ReservationId = reservationId,
            ModifiedBy = modifiedBy
        });

        return res?.FirstOrDefault() ?? 0;
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

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Get
    public async Task<IEnumerable<Payment>> GetPaymentsByOfficeIdsAsync(Guid organizationId, string officeAccess, int paymentDirectionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines, billAllocations) = await db.DapperProcQueryTripleAsync<PaymentEntity, PaymentLedgerLineEntity, PaymentBillAllocationEntity>("Accounting.Payment_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess,
            PaymentDirectionId = paymentDirectionId
        });

        return MapPaymentsWithLedgerLineEntities(headers, lines, billAllocations);
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_GetById", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId
        });

        var payment = MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
        if (payment == null)
            return null;

        var billAllocations = await GetBillAllocationsByPaymentIdAsync(paymentId, organizationId);
        payment.BillAllocations = billAllocations.ToList();
        return payment;
    }

    public async Task<IReadOnlyList<PaymentLedgerLine>> GetLedgerLinesByPaymentIdAsync(Guid paymentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var lineEntities = await db.DapperProcQueryAsync<PaymentLedgerLineEntity>("Accounting.LedgerLine_GetByPaymentId", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId
        });

        return (lineEntities ?? Enumerable.Empty<PaymentLedgerLineEntity>())
            .Select(ConvertPaymentLedgerLineEntityToModel)
            .OrderBy(line => line.LedgerLineDate)
            .ThenBy(line => line.InvoiceCode)
            .ThenBy(line => line.LineNumber)
            .ToList();
    }
    #endregion

    #region Post
    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await CreatePaymentCoreAsync(db, null, payment);
    }

    public Task<Payment> CreatePaymentWithInvoiceAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, Guid currentUser)
        => RunInTransactionAsync(async (db, transaction) =>
        {
            var createdPayment = await CreatePaymentCoreAsync(db, transaction, payment);
            await ApplyExplicitAllocationsCoreAsync(db, transaction, createdPayment, allocations, currentUser);
            return await LoadPaymentByIdCoreAsync(db, transaction, createdPayment.PaymentId, payment.OrganizationId)
                ?? createdPayment;
        });

    public async Task<IReadOnlyList<PaymentBillAllocation>> GetBillAllocationsByReceiptIdAsync(Guid receiptId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<PaymentBillAllocationEntity>("Accounting.PaymentBillAllocation_GetByReceiptId", new
        {
            ReceiptId = receiptId,
            OrganizationId = organizationId
        });

        return (entities ?? Enumerable.Empty<PaymentBillAllocationEntity>())
            .Select(ConvertPaymentBillAllocationEntityToModel)
            .OrderBy(allocation => allocation.LineNumber)
            .ToList();
    }

    public Task<Payment> CreatePaymentWithBillAllocationsAsync(Payment payment, IReadOnlyList<PaymentBillAllocation> allocations, Guid currentUser)
        => RunInTransactionAsync(async (db, transaction) =>
        {
            var createdPayment = await CreatePaymentCoreAsync(db, transaction, payment);
            await ApplyBillAllocationsCoreAsync(db, transaction, createdPayment, allocations, currentUser);
            return await LoadPaymentByIdCoreAsync(db, transaction, createdPayment.PaymentId, payment.OrganizationId)
                ?? createdPayment;
        });

    public Task<Payment> UpdatePaymentWithBillAllocationsAsync(Payment payment, IReadOnlyList<PaymentBillAllocation> allocations, Guid currentUser)
        => RunInTransactionAsync(async (db, transaction) =>
        {
            await db.DapperProcExecuteAsync("Accounting.PaymentBillAllocation_DeleteByPaymentId", new
            {
                PaymentId = payment.PaymentId
            }, transaction: transaction);

            var updatedPayment = await UpdatePaymentCoreAsync(db, transaction, payment);
            await ApplyBillAllocationsCoreAsync(db, transaction, updatedPayment, allocations, currentUser);
            return await LoadPaymentByIdCoreAsync(db, transaction, updatedPayment.PaymentId, payment.OrganizationId)
                ?? updatedPayment;
        });

    public async Task<IReadOnlyList<PaymentBillAllocation>> GetBillAllocationsByPaymentIdAsync(Guid paymentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<PaymentBillAllocationEntity>("Accounting.PaymentBillAllocation_GetByPaymentId", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId
        });

        return (entities ?? Enumerable.Empty<PaymentBillAllocationEntity>())
            .Select(ConvertPaymentBillAllocationEntityToModel)
            .OrderBy(allocation => allocation.LineNumber)
            .ToList();
    }

    public Task<Payment> UpdatePaymentWithInvoiceAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, Guid currentUser)
        => RunInTransactionAsync(async (db, transaction) =>
        {
            await RemovePaymentInvoiceApplicationsCoreAsync(db, transaction, payment.PaymentId, payment.OrganizationId, currentUser);
            var updatedPayment = await UpdatePaymentCoreAsync(db, transaction, payment);
            await ApplyExplicitAllocationsCoreAsync(db, transaction, updatedPayment, allocations, currentUser);
            return await LoadPaymentByIdCoreAsync(db, transaction, updatedPayment.PaymentId, payment.OrganizationId)
                ?? updatedPayment;
        });
    #endregion

    #region Put
    public async Task<Payment> UpdatePaymentAsync(Payment payment)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await UpdatePaymentCoreAsync(db, null, payment);
    }
    #endregion

    #region Delete
    public async Task DeletePaymentByIdAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Payment_DeleteById", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId,
            ModifiedBy = currentUser
        });
    }

    public async Task SetLedgerLinePaymentIdAsync(Guid ledgerLineId, Guid paymentId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await SetLedgerLinePaymentIdCoreAsync(db, null, ledgerLineId, paymentId, modifiedBy);
    }

    public async Task SetPaymentDepositIdAsync(Guid paymentId, Guid organizationId, Guid? depositId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Payment_SetDepositId", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId,
            DepositId = depositId is { } id && id != Guid.Empty ? (Guid?)id : null,
            ModifiedBy = modifiedBy
        });
    }

    public async Task ClearPaymentDepositIdsByDepositIdAsync(Guid organizationId, Guid depositId, Guid modifiedBy)
    {
        if (depositId == Guid.Empty)
            return;

        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Payment_ClearDepositIdByDepositId", new
        {
            OrganizationId = organizationId,
            DepositId = depositId,
            ModifiedBy = modifiedBy
        });
    }
    #endregion

    private async Task<Payment> CreatePaymentCoreAsync(SqlConnection db, IDbTransaction? transaction, Payment payment)
    {
        var paymentCode = payment.PaymentCode?.Trim();
        if (string.IsNullOrWhiteSpace(paymentCode))
            throw new ArgumentException("PaymentCode is required.", nameof(payment));

        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_Add", new
        {
            OrganizationId = payment.OrganizationId,
            OfficeId = payment.OfficeId,
            PaymentCode = paymentCode,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            CostCodeId = payment.CostCodeId,
            Description = payment.Description,
            PaymentDirectionId = payment.PaymentDirectionId,
            PaymentTypeId = payment.PaymentTypeId,
            ChartOfAccountId = payment.ChartOfAccountId is > 0 ? payment.ChartOfAccountId : null,
            PostingStatusId = payment.PostingStatusId ?? 0,
            IsActive = payment.IsActive,
            CreatedBy = payment.CreatedBy
        }, transaction: transaction);

        var created = MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
        if (created == null)
            throw new Exception("Payment record not created");

        return created;
    }

    private async Task<Payment> UpdatePaymentCoreAsync(SqlConnection db, IDbTransaction? transaction, Payment payment)
    {
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_UpdateById", new
        {
            PaymentId = payment.PaymentId,
            OrganizationId = payment.OrganizationId,
            OfficeId = payment.OfficeId,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            CostCodeId = payment.CostCodeId,
            Description = payment.Description,
            PaymentDirectionId = payment.PaymentDirectionId,
            PaymentTypeId = payment.PaymentTypeId,
            ChartOfAccountId = payment.ChartOfAccountId is > 0 ? payment.ChartOfAccountId : null,
            PostingStatusId = payment.PostingStatusId ?? 0,
            IsActive = payment.IsActive,
            ModifiedBy = payment.ModifiedBy
        }, transaction: transaction);

        var updated = MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
        if (updated == null)
            throw new Exception("Payment record not found");

        return updated;
    }

    private async Task<Payment?> LoadPaymentByIdCoreAsync(SqlConnection db, IDbTransaction transaction, Guid paymentId, Guid organizationId)
    {
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_GetById", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId
        }, transaction: transaction);

        return MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
    }

    private async Task<IReadOnlyList<PaymentLedgerLine>> GetLedgerLinesByPaymentIdCoreAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Guid paymentId,
        Guid organizationId)
    {
        var lineEntities = await db.DapperProcQueryAsync<PaymentLedgerLineEntity>("Accounting.LedgerLine_GetByPaymentId", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId
        }, transaction: transaction);

        return (lineEntities ?? Enumerable.Empty<PaymentLedgerLineEntity>())
            .Select(ConvertPaymentLedgerLineEntityToModel)
            .OrderBy(line => line.LedgerLineDate)
            .ThenBy(line => line.InvoiceCode)
            .ThenBy(line => line.LineNumber)
            .ToList();
    }

    private async Task SetLedgerLinePaymentIdCoreAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid ledgerLineId,
        Guid paymentId,
        Guid modifiedBy)
    {
        await db.DapperProcExecuteAsync("Accounting.LedgerLine_SetPaymentId", new
        {
            LedgerLineId = ledgerLineId,
            PaymentId = paymentId,
            ModifiedBy = modifiedBy
        }, transaction: transaction);
    }

    private async Task RemovePaymentInvoiceApplicationsCoreAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Guid paymentId,
        Guid organizationId,
        Guid currentUser)
    {
        var paymentLedgerLines = await GetLedgerLinesByPaymentIdCoreAsync(db, transaction, paymentId, organizationId);

        foreach (var invoiceGroup in paymentLedgerLines.GroupBy(line => line.InvoiceId))
        {
            var invoice = await LoadInvoiceByIdAsync(db, transaction, invoiceGroup.Key, organizationId);
            if (invoice == null)
                continue;

            foreach (var paymentLine in invoiceGroup)
            {
                invoice.PaidAmount -= paymentLine.Amount;
                invoice.LedgerLines.RemoveAll(line => line.LedgerLineId == paymentLine.LedgerLineId);
            }

            invoice.ModifiedBy = currentUser;
            await UpdateByIdCoreAsync(db, transaction, invoice);
        }
    }

    private async Task ApplyExplicitAllocationsCoreAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Payment payment,
        IReadOnlyList<PaymentInvoiceAllocation> allocations,
        Guid currentUser)
    {
        foreach (var allocation in allocations)
        {
            var invoice = await LoadInvoiceByIdAsync(db, transaction, allocation.InvoiceId, payment.OrganizationId);
            if (invoice == null)
                throw new Exception("Invalid Invoice");

            if (invoice.OfficeId != payment.OfficeId)
                throw new Exception("Invoice office does not match payment office.");

            var allocationDescription = string.IsNullOrWhiteSpace(allocation.Description)
                ? payment.Description
                : allocation.Description.Trim();

            invoice.PaidAmount += allocation.Amount;
            var maxLineNumber = invoice.LedgerLines.Any() ? invoice.LedgerLines.Max(ll => ll.LineNumber) : 0;
            var paymentLineNumber = maxLineNumber + 1;
            invoice.LedgerLines.Add(new LedgerLine
            {
                InvoiceId = invoice.InvoiceId,
                LineNumber = paymentLineNumber,
                ReservationId = invoice.ReservationId,
                CostCodeId = payment.CostCodeId,
                Description = allocationDescription,
                Amount = allocation.Amount,
                LedgerLineDate = payment.PaymentDate,
                CreatedBy = currentUser
            });
            invoice.ModifiedBy = currentUser;

            var updatedInvoice = await UpdateByIdCoreAsync(db, transaction, invoice);
            var paymentLedgerLine = updatedInvoice.LedgerLines.Single(line => line.LineNumber == paymentLineNumber);
            await SetLedgerLinePaymentIdCoreAsync(db, transaction, paymentLedgerLine.LedgerLineId, payment.PaymentId, currentUser);
        }
    }

    private async Task ApplyBillAllocationsCoreAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Payment payment,
        IReadOnlyList<PaymentBillAllocation> allocations,
        Guid currentUser)
    {
        await db.DapperProcExecuteAsync("Accounting.PaymentBillAllocation_DeleteByPaymentId", new
        {
            PaymentId = payment.PaymentId
        }, transaction: transaction);

        var lineNumber = 0;
        foreach (var allocation in allocations)
        {
            lineNumber++;
            await db.DapperProcExecuteAsync("Accounting.PaymentBillAllocation_Add", new
            {
                PaymentId = payment.PaymentId,
                ReceiptId = allocation.ReceiptId,
                LineNumber = lineNumber,
                Amount = allocation.Amount,
                CostCodeId = allocation.CostCodeId is > 0 ? allocation.CostCodeId : null,
                Description = string.IsNullOrWhiteSpace(allocation.Description)
                    ? payment.Description
                    : allocation.Description.Trim(),
                CreatedBy = currentUser
            }, transaction: transaction);
        }
    }
}

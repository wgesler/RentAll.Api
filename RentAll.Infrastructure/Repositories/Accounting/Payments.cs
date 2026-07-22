using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Get
    public async Task<IEnumerable<Payment>> GetPaymentsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        return MapPaymentsWithLedgerLineEntities(headers, lines);
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_GetById", new
        {
            PaymentId = paymentId,
            OrganizationId = organizationId
        });

        return MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
    }
    #endregion

    #region Post
    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_Add", new
        {
            OrganizationId = payment.OrganizationId,
            OfficeId = payment.OfficeId,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            CostCodeId = payment.CostCodeId,
            Description = payment.Description,
            PaymentTypeId = payment.PaymentTypeId,
            DepositId = payment.DepositId,
            PostingStatusId = payment.PostingStatusId ?? 0,
            IsActive = payment.IsActive,
            CreatedBy = payment.CreatedBy
        });

        var created = MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
        if (created == null)
            throw new Exception("Payment record not created");

        return created;
    }
    #endregion

    #region Put
    public async Task<Payment> UpdatePaymentAsync(Payment payment)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<PaymentEntity, PaymentLedgerLineEntity>("Accounting.Payment_UpdateById", new
        {
            PaymentId = payment.PaymentId,
            OrganizationId = payment.OrganizationId,
            OfficeId = payment.OfficeId,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            CostCodeId = payment.CostCodeId,
            Description = payment.Description,
            PaymentTypeId = payment.PaymentTypeId,
            DepositId = payment.DepositId,
            PostingStatusId = payment.PostingStatusId ?? 0,
            IsActive = payment.IsActive,
            ModifiedBy = payment.ModifiedBy
        });

        var updated = MapPaymentsWithLedgerLineEntities(headers, lines).FirstOrDefault();
        if (updated == null)
            throw new Exception("Payment record not found");

        return updated;
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
        await db.DapperProcExecuteAsync("Accounting.LedgerLine_SetPaymentId", new
        {
            LedgerLineId = ledgerLineId,
            PaymentId = paymentId,
            ModifiedBy = modifiedBy
        });
    }
    #endregion
}

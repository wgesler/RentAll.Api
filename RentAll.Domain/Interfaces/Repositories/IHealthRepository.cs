using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IHealthRepository
{
    Task<DocumentHealthResult> RunReceiptHealthCheckAsync(Guid organizationId, string officeIds);
    Task<DocumentHealthResult> RunBillHealthCheckAsync(Guid organizationId, string officeIds);
    Task<DocumentHealthResult> RunWorkOrderHealthCheckAsync(Guid organizationId, string officeIds);
    Task<DocumentHealthResult> RunInvoiceHealthCheckAsync(Guid organizationId, string officeIds);
    Task<DocumentHealthResult> RunPaymentHealthCheckAsync(Guid organizationId, string officeIds, int? paymentKindId);
    Task<DocumentHealthResult> RunDepositHealthCheckAsync(Guid organizationId, string officeIds);
    Task<DocumentHealthResult> RunTransferHealthCheckAsync(Guid organizationId, string officeIds);
    Task<DocumentHealthResult> RunManualJournalEntryHealthCheckAsync(Guid organizationId, string officeIds);
}

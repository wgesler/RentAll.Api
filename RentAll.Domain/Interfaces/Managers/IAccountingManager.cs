using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IAccountingManager
{
    Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser);
    Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateOnly invoiceDate, DateOnly startDate, DateOnly endDate);
    Task<List<LedgerLine>> CreateLedgerLinesForOrganizationIdAsync(Organization organization, DateOnly startDate, DateOnly endDate);
    Task CreateDefaultCostCodeAsync(Guid organizationId, int officeId);
}

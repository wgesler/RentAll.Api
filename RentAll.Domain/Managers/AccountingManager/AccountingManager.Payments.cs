namespace RentAll.Domain.Managers;

using RentAll.Domain.Models;

public partial class AccountingManager
{
    public async Task<Payment> ApplyInvoicePaymentAsync(Payment payment, IReadOnlyList<Guid>? autoSplitInvoiceIds, IReadOnlyList<PaymentInvoiceAllocation>? explicitAllocations, string officeAccess, Guid currentUser)
    {
        if (explicitAllocations != null && explicitAllocations.Count > 0)
            return await ApplyInvoicePaymentWithExplicitAllocationsAsync(payment, explicitAllocations, officeAccess, currentUser);

        if (autoSplitInvoiceIds != null && autoSplitInvoiceIds.Count > 0)
            return await ApplyInvoicePaymentWithAutoSplitAsync(payment, autoSplitInvoiceIds, officeAccess, currentUser);

        throw new ArgumentException("At least one invoice or allocation is required.", nameof(autoSplitInvoiceIds));
    }

    private async Task<Payment> ApplyInvoicePaymentWithAutoSplitAsync(Payment payment, IReadOnlyList<Guid> invoiceIds, string officeAccess, Guid currentUser)
    {
        var createdPayment = await _accountingRepository.CreatePaymentAsync(payment);

        var invoicePayment = await ApplyPaymentToInvoicesAsync(
            invoiceIds.ToList(),
            payment.OrganizationId,
            officeAccess,
            payment.CostCodeId,
            payment.Description,
            payment.Amount,
            payment.PaymentDate,
            currentUser);

        await LinkInvoicePaymentApplicationsAsync(createdPayment.PaymentId, invoicePayment, currentUser);
        await CreateJournalEntriesFromPaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    private async Task<Payment> ApplyInvoicePaymentWithExplicitAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser)
    {
        ValidateExplicitPaymentAllocations(payment, allocations);

        Payment? createdPayment = null;
        try
        {
            createdPayment = await _accountingRepository.CreatePaymentWithAllocationsAsync(payment, allocations, currentUser);
            await CreateJournalEntriesFromPaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);
        }
        catch
        {
            if (createdPayment != null)
                await TryDeleteIncompletePaymentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

            throw;
        }

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    private async Task<Payment> UpdateInvoicePaymentWithExplicitAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required.", nameof(payment));

        ValidateExplicitPaymentAllocations(payment, allocations);

        var existing = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, payment.OrganizationId);
        if (existing == null)
            throw new Exception("Payment record not found");

        await DeleteJournalEntriesForPaymentAsync(existing);

        var updatedPayment = await _accountingRepository.UpdatePaymentWithAllocationsAsync(payment, allocations, currentUser);
        await CreateJournalEntriesFromPaymentDocumentAsync(updatedPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(updatedPayment.PaymentId, payment.OrganizationId)
            ?? updatedPayment;
    }

    private static void ValidateExplicitPaymentAllocations(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations)
    {
        if (allocations == null || allocations.Count == 0)
            throw new ArgumentException("At least one invoice allocation is required.", nameof(allocations));

        var allocationTotal = allocations.Sum(allocation => allocation.Amount);
        if (allocationTotal != payment.Amount)
            throw new ArgumentException("Allocation total must equal the payment amount.", nameof(allocations));
    }

    private async Task TryDeleteIncompletePaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        try
        {
            var existing = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (existing != null)
                await DeletePaymentAsync(paymentId, organizationId, currentUser);
        }
        catch
        {
            // Best-effort cleanup after a failed create.
        }
    }

    private static LedgerLine ToInvoicePaymentLedgerLine(PaymentLedgerLine paymentLine)
        => new()
        {
            LedgerLineId = paymentLine.LedgerLineId,
            InvoiceId = paymentLine.InvoiceId,
            LineNumber = paymentLine.LineNumber,
            ReservationId = paymentLine.ReservationId,
            CostCodeId = paymentLine.CostCodeId,
            Amount = paymentLine.Amount,
            Description = paymentLine.Description,
            LedgerLineDate = paymentLine.LedgerLineDate,
            PaymentId = paymentLine.PaymentId
        };

    private async Task LinkInvoicePaymentApplicationsAsync(Guid paymentId, InvoicePayment invoicePayment, Guid currentUser)
    {
        foreach (var application in invoicePayment.PaymentApplications)
        {
            await _accountingRepository.SetLedgerLinePaymentIdAsync(
                application.PaymentLedgerLine.LedgerLineId,
                paymentId,
                currentUser);
            application.PaymentLedgerLine.PaymentId = paymentId;
        }
    }
}

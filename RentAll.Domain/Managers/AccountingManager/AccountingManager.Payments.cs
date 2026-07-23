namespace RentAll.Domain.Managers;

using RentAll.Domain.Models;

public partial class AccountingManager
{
    public Task<Payment> CreatePaymentWithInvoiceAllocationsAsync(
        Payment payment,
        IReadOnlyList<PaymentInvoiceAllocation> allocations,
        string officeAccess,
        Guid currentUser)
        => ApplyInvoicePaymentAsync(payment, null, allocations, officeAccess, currentUser);

    public async Task<Payment> ApplyInvoicePaymentAsync(
        Payment payment,
        IReadOnlyList<Guid>? autoSplitInvoiceIds,
        IReadOnlyList<PaymentInvoiceAllocation>? explicitAllocations,
        string officeAccess,
        Guid currentUser)
    {
        if (explicitAllocations != null && explicitAllocations.Count > 0)
            return await ApplyInvoicePaymentWithExplicitAllocationsAsync(payment, explicitAllocations, officeAccess, currentUser);

        if (autoSplitInvoiceIds != null && autoSplitInvoiceIds.Count > 0)
            return await ApplyInvoicePaymentWithAutoSplitAsync(payment, autoSplitInvoiceIds, officeAccess, currentUser);

        throw new ArgumentException("At least one invoice or allocation is required.", nameof(autoSplitInvoiceIds));
    }

    private async Task<Payment> ApplyInvoicePaymentWithAutoSplitAsync(
        Payment payment,
        IReadOnlyList<Guid> invoiceIds,
        string officeAccess,
        Guid currentUser)
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

    private async Task<Payment> ApplyInvoicePaymentWithExplicitAllocationsAsync(
        Payment payment,
        IReadOnlyList<PaymentInvoiceAllocation> allocations,
        string officeAccess,
        Guid currentUser)
    {
        if (allocations == null || allocations.Count == 0)
            throw new ArgumentException("At least one invoice allocation is required.", nameof(allocations));

        var allocationTotal = allocations.Sum(allocation => allocation.Amount);
        if (allocationTotal != payment.Amount)
            throw new ArgumentException("Allocation total must equal the payment amount.", nameof(allocations));

        var createdPayment = await _accountingRepository.CreatePaymentAsync(payment);

        foreach (var allocation in allocations)
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(allocation.InvoiceId, payment.OrganizationId);
            if (invoice == null)
                throw new Exception("Invalid Invoice");

            if (invoice.OfficeId != payment.OfficeId)
                throw new Exception("Invoice office does not match payment office.");

            var allocationDescription = string.IsNullOrWhiteSpace(allocation.Description)
                ? payment.Description
                : allocation.Description.Trim();

            var invoicePayment = await ApplyPaymentToInvoicesAsync(
                new List<Guid> { allocation.InvoiceId },
                payment.OrganizationId,
                officeAccess,
                payment.CostCodeId,
                allocationDescription,
                allocation.Amount,
                payment.PaymentDate,
                currentUser);

            await LinkInvoicePaymentApplicationsAsync(createdPayment.PaymentId, invoicePayment, currentUser);
        }

        await CreateJournalEntriesFromPaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    public async Task DeletePaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required.", nameof(paymentId));

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
            throw new Exception("Payment record not found");

        var paymentLedgerLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(paymentId, organizationId);

        await DeleteJournalEntriesForPaymentAsync(payment);

        foreach (var invoiceGroup in paymentLedgerLines.GroupBy(line => line.InvoiceId))
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceGroup.Key, organizationId);
            if (invoice == null)
                continue;

            foreach (var paymentLine in invoiceGroup)
            {
                await DeleteJournalEntriesForInvoicePaymentLedgerLineAsync(invoice, ToInvoicePaymentLedgerLine(paymentLine));
                invoice.PaidAmount -= paymentLine.Amount;
                invoice.LedgerLines.RemoveAll(line => line.LedgerLineId == paymentLine.LedgerLineId);
            }

            invoice.ModifiedBy = currentUser;
            await _accountingRepository.UpdateByIdAsync(invoice);
        }

        await _accountingRepository.DeletePaymentByIdAsync(paymentId, organizationId, currentUser);
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

    private async Task LinkInvoicePaymentApplicationsAsync(
        Guid paymentId,
        InvoicePayment invoicePayment,
        Guid currentUser)
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

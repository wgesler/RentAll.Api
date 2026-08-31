using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Payment Document
    public async Task<Payment> CreatePaymentWithBillAllocationsAsync(Payment payment, IReadOnlyList<PaymentBillAllocation> allocations, Guid currentUser)
    {
        EnsureBillPayment(payment);

        if (payment.ChartOfAccountId is not > 0)
            throw new ArgumentException("ChartOfAccountId is required.", nameof(payment));

        ValidateExplicitBillPaymentAllocations(payment, allocations);

        var preparedApplications = await PrepareBillPaymentApplicationsAsync(payment, allocations, currentUser);
        payment.CostCodeId = await ResolveBillPaymentHeaderCostCodeIdAsync(payment, preparedApplications);

        Payment? createdPayment = null;
        try
        {
            await EnsurePaymentCodeAsync(payment);
            createdPayment = await _accountingRepository.CreatePaymentWithBillAllocationsAsync(payment, allocations, currentUser);
            await ApplyPreparedBillPaymentApplicationsAsync(preparedApplications, payment.PaymentTypeId, currentUser);
            await CreateJournalEntriesFromBillPaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);
        }
        catch
        {
            if (createdPayment != null)
                await TryDeleteIncompleteBillPaymentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

            throw;
        }

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment!.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    public async Task<Payment> UpdatePaymentWithBillAllocationsAsync(Payment payment, IReadOnlyList<PaymentBillAllocation> allocations, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required.", nameof(payment));

        EnsureBillPayment(payment);

        if (payment.ChartOfAccountId is not > 0)
            throw new ArgumentException("ChartOfAccountId is required.", nameof(payment));

        ValidateExplicitBillPaymentAllocations(payment, allocations);

        var existing = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, payment.OrganizationId)
            ?? throw new Exception("Payment record not found");

        if (existing.PaymentKindId != (int)PaymentKind.Bill)
            throw new Exception("Payment is not a bill payment.");

        payment.PaymentCode = existing.PaymentCode;
        payment.DepositId = existing.DepositId;
        payment.PostingStatusId = existing.PostingStatusId;

        var preparedApplications = await PrepareBillPaymentApplicationsAsync(payment, allocations, currentUser);
        payment.CostCodeId = await ResolveBillPaymentHeaderCostCodeIdAsync(payment, preparedApplications);

        await ClearPaymentDocumentLinksAsync(existing.OrganizationId, existing.PaymentId, currentUser);
        await DeleteJournalEntriesForPaymentAsync(existing);
        await ReverseBillPaymentApplicationsAsync(existing, currentUser);

        var updatedPayment = await _accountingRepository.UpdatePaymentWithBillAllocationsAsync(payment, allocations, currentUser);
        await ApplyPreparedBillPaymentApplicationsAsync(preparedApplications, payment.PaymentTypeId, currentUser);
        await CreateJournalEntriesFromBillPaymentDocumentAsync(updatedPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(updatedPayment.PaymentId, payment.OrganizationId)
            ?? updatedPayment;
    }

    private static void EnsureBillPayment(Payment payment)
    {
        if (payment.PaymentKindId != (int)PaymentKind.Bill)
            throw new ArgumentException("Bill allocations are only supported for bill payments.", nameof(payment));
    }

    private static void ValidateExplicitBillPaymentAllocations(Payment payment, IReadOnlyList<PaymentBillAllocation> allocations)
    {
        if (allocations == null || allocations.Count == 0)
            throw new ArgumentException("At least one bill allocation is required.", nameof(allocations));

        var allocationTotal = allocations.Sum(allocation => allocation.Amount);
        if (allocationTotal != payment.Amount)
            throw new ArgumentException("Allocation total must equal the payment amount.", nameof(allocations));
    }

    private async Task<int> ResolveBillPaymentHeaderCostCodeIdAsync(Payment payment, IReadOnlyList<BillPaymentApplication> applications)
    {
        var costCodeId = applications
            .Select(application => application.CostCodeId)
            .FirstOrDefault(value => value is > 0);

        if (costCodeId is > 0)
            return costCodeId.Value;

        var costCodeById = await LoadCostCodeByOfficeIdAsync(payment.OrganizationId, payment.OfficeId);
        var accountsPayableFallback = costCodeById.Values
            .Where(costCode => costCode.OfficeId == payment.OfficeId && costCode.IsActive)
            .FirstOrDefault(costCode => NormalizeAccountCode(costCode.Code)
                .Equals("2000", StringComparison.OrdinalIgnoreCase));

        if (accountsPayableFallback?.CostCodeId is > 0)
            return accountsPayableFallback.CostCodeId;

        var firstActive = costCodeById.Values
            .FirstOrDefault(costCode => costCode.OfficeId == payment.OfficeId && costCode.IsActive);

        if (firstActive?.CostCodeId is > 0)
            return firstActive.CostCodeId;

        throw new Exception("Unable to resolve a cost code for the bill payment. Add cost codes for this office or map expense accounts on the bills.");
    }

    private async Task<List<BillPaymentApplication>> PrepareBillPaymentApplicationsAsync(Payment payment, IReadOnlyList<PaymentBillAllocation> allocations, Guid currentUser)
    {
        var applications = new List<BillPaymentApplication>();
        var lineNumber = 0;

        foreach (var allocation in allocations)
        {
            lineNumber++;
            if (allocation.ReceiptId == Guid.Empty)
                throw new Exception("ReceiptId is required for each bill allocation.");

            var bill = await _maintenanceRepository.GetReceiptByIdAsync(allocation.ReceiptId, payment.OrganizationId)
                ?? throw new Exception("Invalid Bill");

            if (!bill.IsActive)
                throw new Exception($"Bill {bill.ReceiptCode} is inactive.");

            if (bill.OfficeId != payment.OfficeId)
                throw new Exception($"Bill {bill.ReceiptCode} office does not match payment office.");

            bill = await LoadReceiptWithSplitsAsync(bill);
            var resolvedCostCodeId = allocation.CostCodeId is > 0
                ? allocation.CostCodeId
                : await ResolvePrimaryCostCodeIdFromBillAsync(bill);

            allocation.LineNumber = lineNumber;
            allocation.CostCodeId = resolvedCostCodeId;
            allocation.Description = ResolveBillPaymentAllocationDescription(bill, allocation.Description, payment.Description);

            applications.Add(new BillPaymentApplication
            {
                Bill = bill,
                AmountApplied = allocation.Amount,
                PaymentDate = payment.PaymentDate,
                ChartOfAccountId = payment.ChartOfAccountId!.Value,
                Description = allocation.Description,
                CostCodeId = resolvedCostCodeId
            });
        }

        return applications;
    }

    private async Task ApplyPreparedBillPaymentApplicationsAsync(IReadOnlyList<BillPaymentApplication> applications, int? paymentTypeId, Guid currentUser)
    {
        if (applications.Count == 0)
            return;

        var billsToUpdate = new List<Receipt>();
        foreach (var application in applications)
        {
            var bill = application.Bill!;
            bill.PaidAmount += application.AmountApplied;
            bill.PaidDate = application.PaymentDate;
            bill.PaymentDescription = application.Description;
            bill.PaymentTypeId = paymentTypeId ?? bill.PaymentTypeId;
            bill.CheckPrinted = false;
            bill.ModifiedBy = currentUser;
            bill.ModifiedOn = DateTimeOffset.UtcNow;
            billsToUpdate.Add(bill);
        }

        var updatedBills = await _maintenanceRepository.UpdateReceiptsInTransactionAsync(billsToUpdate);
        for (var index = 0; index < applications.Count; index++)
        {
            var updatedBill = updatedBills.Single(bill => bill.ReceiptId == applications[index].Bill!.ReceiptId);
            applications[index].Bill = updatedBill;
        }
    }

    private async Task TryDeleteIncompleteBillPaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        try
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment == null)
                return;

            await ReverseBillPaymentApplicationsAsync(payment, currentUser);
            await DeletePaymentAsync(paymentId, organizationId, currentUser);
        }
        catch
        {
            // Best-effort cleanup after a failed create.
        }
    }

    private async Task ReverseBillPaymentApplicationsAsync(Payment payment, Guid currentUser)
    {
        if (payment.BillAllocations.Count == 0)
            payment = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, payment.OrganizationId) ?? payment;

        if (payment.BillAllocations.Count == 0)
            return;

        var billsToUpdate = new List<Receipt>();
        foreach (var allocation in payment.BillAllocations)
        {
            var bill = await _maintenanceRepository.GetReceiptByIdAsync(allocation.ReceiptId, payment.OrganizationId);
            if (bill == null)
                continue;

            bill.PaidAmount -= allocation.Amount;
            bill.ModifiedBy = currentUser;
            bill.ModifiedOn = DateTimeOffset.UtcNow;
            billsToUpdate.Add(bill);
        }

        if (billsToUpdate.Count > 0)
            await _maintenanceRepository.UpdateReceiptsInTransactionAsync(billsToUpdate);
    }
    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Reconcile Invalidation
    private static bool ShouldInvalidateReconcileOnDocumentEdit(PostingStatus postingStatus)
        => postingStatus is PostingStatus.Posted or PostingStatus.SoftClosed;

    private static PostingStatus ResolveDocumentPostingStatus(int? postingStatusId)
        => postingStatusId is >= 0 and <= (int)PostingStatus.HardClosed
            ? (PostingStatus)postingStatusId.Value
            : PostingStatus.Open;

    private async Task<int> ApplySourceDocumentEditReconcileInvalidationAsync(int? existingPostingStatusId, Guid organizationId, int officeId, DateOnly transactionDate, DateOnly accountingPeriod, Guid currentUser, Func<Task<IReadOnlyCollection<JournalEntry>>> loadJournalEntriesAsync)
    {
        var existingStatus = ResolveDocumentPostingStatus(existingPostingStatusId);
        var postingStatusId = (int)existingStatus;

        if (ShouldInvalidateReconcileOnDocumentEdit(existingStatus))
        {
            var journalEntries = (await loadJournalEntriesAsync())
                .GroupBy(entry => entry.JournalEntryId)
                .Select(group => group.First())
                .ToList();

            if (journalEntries.Count > 0)
            {
                var affectedAccountIds = await _journalEntryRepository.ClearReconcileMarksByJournalEntryIdsAsync(organizationId, officeId, journalEntries.Select(entry => entry.JournalEntryId), currentUser);

                await AdjustReconcileAccountBalancesAfterInvalidationAsync(organizationId, officeId, affectedAccountIds);

                foreach (var journalEntry in journalEntries.Where(entry => entry.PostingStatusId is PostingStatus.Posted or PostingStatus.SoftClosed))
                    await ResetJournalEntryPostingStatusToOpenAsync(journalEntry.JournalEntryId, organizationId, currentUser);
            }

            postingStatusId = (int)PostingStatus.Open;
        }

        return await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(organizationId, officeId, transactionDate, accountingPeriod, postingStatusId);
    }

    private async Task ApplyJournalEntryEditReconcileInvalidationAsync(JournalEntry existingJournalEntry, Guid currentUser)
    {
        if (!ShouldInvalidateReconcileOnDocumentEdit(existingJournalEntry.PostingStatusId))
            return;

        var affectedAccountIds = await _journalEntryRepository.ClearReconcileMarksByJournalEntryIdsAsync(existingJournalEntry.OrganizationId, existingJournalEntry.OfficeId, [existingJournalEntry.JournalEntryId], currentUser);

        await AdjustReconcileAccountBalancesAfterInvalidationAsync(existingJournalEntry.OrganizationId, existingJournalEntry.OfficeId, affectedAccountIds);

        if (existingJournalEntry.PostingStatusId is PostingStatus.Posted or PostingStatus.SoftClosed)
            await ResetJournalEntryPostingStatusToOpenAsync(existingJournalEntry.JournalEntryId, existingJournalEntry.OrganizationId, currentUser);
    }

    private async Task AdjustReconcileAccountBalancesAfterInvalidationAsync(Guid organizationId, int officeId, IReadOnlyCollection<int> chartOfAccountIds)
    {
        foreach (var chartOfAccountId in chartOfAccountIds.Distinct().Where(id => id > 0))
        {
            var latestReconcile = await GetLatestReconciliationAsync(organizationId, officeId, chartOfAccountId);
            if (latestReconcile?.StatementDate is not DateOnly statementDate)
                continue;

            var registerBalance = await _journalEntryRepository.GetReconcileRegisterBalanceAsync(organizationId, officeId, chartOfAccountId, statementDate);

            await _accountingRepository.UpdateChartOfAccountReconcileByIdAsync(organizationId, officeId, chartOfAccountId, registerBalance, statementDate);
        }
    }

    private async Task ResetJournalEntryPostingStatusToOpenAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry?.PostingStatusId is not (PostingStatus.Posted or PostingStatus.SoftClosed))
            return;

        journalEntry.PostingStatusId = PostingStatus.Open;
        journalEntry.ModifiedBy = currentUser;
        await ApplyAccountingOfficeClosedPostingStatusComplianceAsync(journalEntry);
        await _journalEntryRepository.UpdateJournalEntryByIdAsync(journalEntry);
    }

    private async Task<IReadOnlyCollection<JournalEntry>> LoadJournalEntriesForPaymentDocumentAsync(Guid organizationId, Payment payment)
    {
        var journalEntries = new Dictionary<Guid, JournalEntry>();

        foreach (var entry in await GetJournalEntriesByPaymentIdCachedAsync(organizationId, payment.PaymentId))
            journalEntries[entry.JournalEntryId] = entry;

        var sourceType = payment.PaymentKindId switch
        {
            (int)PaymentKind.Bill => SourceType.BillPayment,
            (int)PaymentKind.Owner => SourceType.OwnerDistribution,
            _ => SourceType.InvoicePayment
        };

        foreach (var entry in await GetDocumentJournalEntriesForSyncAsync(organizationId, payment.OfficeId, sourceType, payment.PaymentId))
            journalEntries[entry.JournalEntryId] = entry;

        return journalEntries.Values.ToList();
    }

    private async Task<IReadOnlyCollection<JournalEntry>> LoadJournalEntriesForDepositDocumentAsync(Guid organizationId, Deposit deposit)
    {
        var journalEntries = new Dictionary<Guid, JournalEntry>();

        foreach (var entry in await GetJournalEntriesByDepositIdCachedAsync(organizationId, deposit.DepositId))
            journalEntries[entry.JournalEntryId] = entry;

        foreach (var entry in await GetDocumentJournalEntriesForSyncAsync(organizationId, deposit.OfficeId, SourceType.Deposit, deposit.DepositId))
            journalEntries[entry.JournalEntryId] = entry;

        return journalEntries.Values.ToList();
    }

    private async Task<IReadOnlyCollection<JournalEntry>> LoadJournalEntriesForTransferDocumentAsync(Guid organizationId, Transfer transfer)
    {
        var journalEntries = new Dictionary<Guid, JournalEntry>();

        foreach (var entry in await GetJournalEntriesByTransferIdCachedAsync(organizationId, transfer.TransferId))
            journalEntries[entry.JournalEntryId] = entry;

        foreach (var entry in await GetDocumentJournalEntriesForSyncAsync(organizationId, transfer.OfficeId, SourceType.Transfer, transfer.TransferId))
            journalEntries[entry.JournalEntryId] = entry;

        return journalEntries.Values.ToList();
    }

    private async Task<IReadOnlyCollection<JournalEntry>> LoadJournalEntriesForReceiptDocumentAsync(Guid organizationId, Receipt receipt)
    {
        var journalEntries = new Dictionary<Guid, JournalEntry>();

        foreach (var sourceType in new[] { SourceType.Bill, SourceType.BillPayment, SourceType.Receipt })
        {
            foreach (var entry in await GetDocumentJournalEntriesForSyncAsync(organizationId, receipt.OfficeId, sourceType, receipt.ReceiptId))
                journalEntries[entry.JournalEntryId] = entry;
        }

        var allocations = await _accountingRepository.GetBillAllocationsByReceiptIdAsync(receipt.ReceiptId, organizationId);
        foreach (var paymentId in allocations.Select(allocation => allocation.PaymentId).Distinct())
        {
            if (paymentId == Guid.Empty)
                continue;

            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment == null)
                continue;

            foreach (var entry in await LoadJournalEntriesForPaymentDocumentAsync(organizationId, payment))
                journalEntries[entry.JournalEntryId] = entry;
        }

        return journalEntries.Values.ToList();
    }

    private async Task<IReadOnlyCollection<JournalEntry>> LoadJournalEntriesForInvoiceDocumentAsync(Guid organizationId, Invoice invoice)
    {
        var journalEntries = new Dictionary<Guid, JournalEntry>();

        foreach (var entry in await GetDocumentJournalEntriesForSyncAsync(organizationId, invoice.OfficeId, SourceType.Invoice, invoice.InvoiceId))
            journalEntries[entry.JournalEntryId] = entry;

        return journalEntries.Values.ToList();
    }

    private async Task<IReadOnlyCollection<JournalEntry>> LoadJournalEntriesForWorkOrderDocumentAsync(Guid organizationId, WorkOrder workOrder)
    {
        var journalEntries = new Dictionary<Guid, JournalEntry>();

        foreach (var entry in await GetDocumentJournalEntriesForSyncAsync(organizationId, workOrder.OfficeId, SourceType.WorkOrder, workOrder.WorkOrderId))
            journalEntries[entry.JournalEntryId] = entry;

        return journalEntries.Values.ToList();
    }
    #endregion
}

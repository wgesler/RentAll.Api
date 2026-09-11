using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Reconcile Documents
    public async Task ApplyReconcileClearPostingAsync(IReadOnlyList<ReconcileJournalEntryLineMark> lines, Guid organizationId, Guid currentUser)
    {
        var targets = await ResolveClearedLineTargetsAsync(lines, organizationId);
        if (targets.SourceKeys.Count == 0 && targets.DirectPostJournalEntryIds.Count == 0)
            return;

        foreach (var (sourceType, sourceId) in targets.SourceKeys)
            await MarkDocumentPostedFromReconcileAsync(sourceType, sourceId, organizationId, currentUser);

        foreach (var journalEntryId in targets.DirectPostJournalEntryIds)
            await PostJournalEntryIfOpenAsync(journalEntryId, organizationId, currentUser);
    }

    public async Task ApplyReconcileCompleteSoftCloseAsync(IReadOnlyList<ReconcileJournalEntryLineMark> lines, Guid organizationId, Guid currentUser)
    {
        var targets = await ResolveClearedLineTargetsAsync(lines, organizationId);
        if (targets.SourceKeys.Count == 0 && targets.AllJournalEntryIds.Count == 0)
            return;

        foreach (var journalEntryId in targets.AllJournalEntryIds)
            await SoftCloseJournalEntryIfEligibleAsync(journalEntryId, organizationId, currentUser);

        foreach (var (sourceType, sourceId) in targets.SourceKeys)
            await MarkDocumentSoftClosedFromReconcileCompleteAsync(sourceType, sourceId, organizationId, currentUser);
    }

    private async Task<ClearedReconcileLineTargets> ResolveClearedLineTargetsAsync(
        IReadOnlyList<ReconcileJournalEntryLineMark> lines,
        Guid organizationId)
    {
        var result = new ClearedReconcileLineTargets();
        if (lines.Count == 0)
            return result;

        var clearedLineIds = lines
            .Where(line => line.IsCleared && line.JournalEntryLineId != Guid.Empty)
            .Select(line => line.JournalEntryLineId)
            .Distinct()
            .ToList();
        if (clearedLineIds.Count == 0)
            return result;

        foreach (var lineId in clearedLineIds)
        {
            var line = await _journalEntryRepository.GetJournalEntryLineByIdAsync(lineId);
            if (line == null || line.JournalEntryId == Guid.Empty)
                continue;

            result.AllJournalEntryIds.Add(line.JournalEntryId);

            var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(line.JournalEntryId, organizationId);
            if (journalEntry == null)
                continue;

            if (ShouldPostJournalEntryDirectlyFromReconcile(journalEntry))
            {
                result.DirectPostJournalEntryIds.Add(journalEntry.JournalEntryId);
                continue;
            }

            if (journalEntry.SourceId is not { } sourceId || sourceId == Guid.Empty)
                continue;
            if (journalEntry.SourceTypeId is not int sourceTypeId || sourceTypeId <= 0)
                continue;

            result.SourceKeys.Add(((SourceType)sourceTypeId, sourceId));
        }

        return result;
    }

    private static bool ShouldPostJournalEntryDirectlyFromReconcile(JournalEntry journalEntry)
    {
        if (journalEntry.SourceId is not { } sourceId || sourceId == Guid.Empty)
            return true;

        var sourceType = journalEntry.SourceTypeId is int sourceTypeId && sourceTypeId >= 0
            ? (SourceType)sourceTypeId
            : SourceType.Journal;

        return sourceType switch
        {
            SourceType.InvoicePayment => false,
            SourceType.Deposit => false,
            SourceType.Transfer => false,
            SourceType.Invoice => false,
            SourceType.Bill => false,
            SourceType.BillPayment => false,
            SourceType.Receipt => false,
            _ => true
        };
    }

    private async Task PostJournalEntryIfOpenAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry?.PostingStatusId != PostingStatus.Open)
            return;

        await PostJournalEntryAsync(journalEntryId, organizationId, currentUser);
    }

    private async Task SoftCloseJournalEntryIfEligibleAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry?.PostingStatusId is PostingStatus.SoftClosed or PostingStatus.HardClosed)
            return;

        await SoftCloseJournalEntryAsync(journalEntryId, organizationId, currentUser);
    }

    private async Task HardCloseJournalEntryIfEligibleAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry?.PostingStatusId == PostingStatus.HardClosed)
            return;

        await HardCloseJournalEntryAsync(journalEntryId, organizationId, currentUser);
    }

    private async Task MarkDocumentPostedFromReconcileAsync(SourceType sourceType, Guid sourceId, Guid organizationId, Guid currentUser)
    {
        switch (sourceType)
        {
            case SourceType.InvoicePayment:
                await MarkPaymentPostedFromReconcileAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Deposit:
                await MarkDepositPostedFromReconcileAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Transfer:
                await MarkTransferPostedFromReconcileAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Invoice:
                await MarkInvoicePostedFromReconcileAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Bill:
            case SourceType.Receipt:
                await MarkReceiptPostedFromReconcileAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.BillPayment:
                await MarkBillPaymentPostedFromReconcileAsync(sourceId, organizationId, currentUser);
                break;
        }
    }

    private async Task MarkDocumentSoftClosedFromReconcileCompleteAsync(SourceType sourceType, Guid sourceId, Guid organizationId, Guid currentUser)
    {
        switch (sourceType)
        {
            case SourceType.InvoicePayment:
                await MarkPaymentSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Deposit:
                await MarkDepositSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Transfer:
                await MarkTransferSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Invoice:
                await MarkInvoiceSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Bill:
            case SourceType.Receipt:
                await MarkReceiptSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.BillPayment:
                await MarkBillPaymentSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
        }
    }

    private async Task MarkPaymentPostedFromReconcileAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || !CanMarkDocumentPostedFromReconcile(payment.PostingStatusId))
            return;

        payment.PostingStatusId = (int)PostingStatus.Posted;
        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
        await PostOpenJournalEntriesForPaymentAsync(payment, organizationId, currentUser);
    }

    private async Task MarkPaymentSoftClosedFromReconcileCompleteAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || !CanSoftCloseDocumentFromReconcileComplete(payment.PostingStatusId))
            return;

        payment.PostingStatusId = (int)PostingStatus.SoftClosed;
        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
    }

    private async Task PostOpenJournalEntriesForPaymentAsync(Payment payment, Guid organizationId, Guid currentUser)
    {
        await PostOpenJournalEntriesForSourceAsync(
            payment.OrganizationId,
            payment.OfficeId,
            SourceType.InvoicePayment,
            payment.PaymentId,
            organizationId,
            currentUser);

        var paymentWithLines = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, organizationId);
        if (paymentWithLines == null)
            return;

        foreach (var paymentLine in paymentWithLines.LedgerLines.Where(line => line.LedgerLineId != Guid.Empty && line.Amount != 0))
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, organizationId);
            if (invoice == null)
                continue;

            var paymentLedgerLine = invoice.LedgerLines.SingleOrDefault(line => line.LedgerLineId == paymentLine.LedgerLineId);
            if (paymentLedgerLine == null)
                continue;

            var paymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                invoice,
                paymentLedgerLine);
            foreach (var entry in paymentEntries.Where(entry => entry.PostingStatusId == PostingStatus.Open))
                await PostJournalEntryAsync(entry.JournalEntryId, organizationId, currentUser);
        }
    }

    private async Task MarkDepositPostedFromReconcileAsync(Guid depositId, Guid organizationId, Guid currentUser)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null || !CanMarkDocumentPostedFromReconcile(deposit.PostingStatusId))
            return;

        deposit.PostingStatusId = (int)PostingStatus.Posted;
        deposit.ModifiedBy = currentUser;
        await _accountingRepository.UpdateDepositAsync(deposit);
        await PostOpenJournalEntriesForSourceAsync(deposit.OrganizationId, deposit.OfficeId, SourceType.Deposit, depositId, organizationId, currentUser);
    }

    private async Task MarkDepositSoftClosedFromReconcileCompleteAsync(Guid depositId, Guid organizationId, Guid currentUser)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null || !CanSoftCloseDocumentFromReconcileComplete(deposit.PostingStatusId))
            return;

        deposit.PostingStatusId = (int)PostingStatus.SoftClosed;
        deposit.ModifiedBy = currentUser;
        await _accountingRepository.UpdateDepositAsync(deposit);
    }

    private async Task MarkTransferPostedFromReconcileAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
        if (transfer == null || !CanMarkDocumentPostedFromReconcile(transfer.PostingStatusId))
            return;

        transfer.PostingStatusId = (int)PostingStatus.Posted;
        transfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(transfer);
        await PostOpenJournalEntriesForSourceAsync(transfer.OrganizationId, transfer.OfficeId, SourceType.Transfer, transferId, organizationId, currentUser);
    }

    private async Task MarkTransferSoftClosedFromReconcileCompleteAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
        if (transfer == null || !CanSoftCloseDocumentFromReconcileComplete(transfer.PostingStatusId))
            return;

        transfer.PostingStatusId = (int)PostingStatus.SoftClosed;
        transfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(transfer);
    }

    private async Task MarkInvoicePostedFromReconcileAsync(Guid invoiceId, Guid organizationId, Guid currentUser)
    {
        var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, organizationId);
        if (invoice == null || !CanMarkDocumentPostedFromReconcile(invoice.PostingStatusId))
            return;

        invoice.PostingStatusId = (int)PostingStatus.Posted;
        invoice.ModifiedBy = currentUser;
        await _accountingRepository.UpdateByIdAsync(invoice);
        await PostOpenJournalEntriesForSourceAsync(invoice.OrganizationId, invoice.OfficeId, SourceType.Invoice, invoiceId, organizationId, currentUser);
    }

    private async Task MarkInvoiceSoftClosedFromReconcileCompleteAsync(Guid invoiceId, Guid organizationId, Guid currentUser)
    {
        var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, organizationId);
        if (invoice == null || !CanSoftCloseDocumentFromReconcileComplete(invoice.PostingStatusId))
            return;

        invoice.PostingStatusId = (int)PostingStatus.SoftClosed;
        invoice.ModifiedBy = currentUser;
        await _accountingRepository.UpdateByIdAsync(invoice);
    }

    private async Task MarkReceiptPostedFromReconcileAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, organizationId);
        if (receipt == null || !CanMarkDocumentPostedFromReconcile(receipt.PostingStatusId))
            return;

        receipt.PostingStatusId = (int)PostingStatus.Posted;
        receipt.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateReceiptAsync(receipt);
        await PostOpenJournalEntriesForReceiptDocumentAsync(receipt, organizationId, currentUser);
    }

    private async Task MarkReceiptSoftClosedFromReconcileCompleteAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, organizationId);
        if (receipt == null || !CanSoftCloseDocumentFromReconcileComplete(receipt.PostingStatusId))
            return;

        receipt.PostingStatusId = (int)PostingStatus.SoftClosed;
        receipt.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateReceiptAsync(receipt);
    }

    private async Task MarkBillPaymentPostedFromReconcileAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        await MarkReceiptPostedFromReconcileAsync(receiptId, organizationId, currentUser);
        await MarkLinkedBillPaymentsPostedFromReconcileAsync(receiptId, organizationId, currentUser);
    }

    private async Task MarkBillPaymentSoftClosedFromReconcileCompleteAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        await MarkReceiptSoftClosedFromReconcileCompleteAsync(receiptId, organizationId, currentUser);
        await MarkLinkedBillPaymentsSoftClosedFromReconcileCompleteAsync(receiptId, organizationId, currentUser);
    }

    private async Task MarkLinkedBillPaymentsPostedFromReconcileAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        var allocations = await _accountingRepository.GetBillAllocationsByReceiptIdAsync(receiptId, organizationId);
        foreach (var paymentId in allocations.Select(allocation => allocation.PaymentId).Distinct())
        {
            if (paymentId == Guid.Empty)
                continue;

            await MarkPaymentPostedFromReconcileAsync(paymentId, organizationId, currentUser);
        }
    }

    private async Task MarkLinkedBillPaymentsSoftClosedFromReconcileCompleteAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        var allocations = await _accountingRepository.GetBillAllocationsByReceiptIdAsync(receiptId, organizationId);
        foreach (var paymentId in allocations.Select(allocation => allocation.PaymentId).Distinct())
        {
            if (paymentId == Guid.Empty)
                continue;

            await MarkPaymentSoftClosedFromReconcileCompleteAsync(paymentId, organizationId, currentUser);
        }
    }

    private async Task PostOpenJournalEntriesForReceiptDocumentAsync(Receipt receipt, Guid organizationId, Guid currentUser)
    {
        await PostOpenJournalEntriesForSourceAsync(receipt.OrganizationId, receipt.OfficeId, SourceType.Bill, receipt.ReceiptId, organizationId, currentUser);
        await PostOpenJournalEntriesForSourceAsync(receipt.OrganizationId, receipt.OfficeId, SourceType.BillPayment, receipt.ReceiptId, organizationId, currentUser);
        await PostOpenJournalEntriesForSourceAsync(receipt.OrganizationId, receipt.OfficeId, SourceType.Receipt, receipt.ReceiptId, organizationId, currentUser);
    }

    private static bool CanMarkDocumentPostedFromReconcile(int? postingStatusId)
    {
        var postingStatus = postingStatusId is >= 0 and <= (int)PostingStatus.HardClosed
            ? (PostingStatus)postingStatusId.Value
            : PostingStatus.Open;

        return postingStatus == PostingStatus.Open;
    }

    private static bool CanSoftCloseDocumentFromReconcileComplete(int? postingStatusId)
    {
        var postingStatus = postingStatusId is >= 0 and <= (int)PostingStatus.HardClosed
            ? (PostingStatus)postingStatusId.Value
            : PostingStatus.Open;

        return postingStatus is PostingStatus.Open or PostingStatus.Posted;
    }

    private static bool CanHardCloseDocumentFromAccountingOfficeClose(int? postingStatusId)
    {
        var postingStatus = postingStatusId is >= 0 and <= (int)PostingStatus.HardClosed
            ? (PostingStatus)postingStatusId.Value
            : PostingStatus.Open;

        return postingStatus is PostingStatus.Open or PostingStatus.Posted or PostingStatus.SoftClosed;
    }

    private async Task MarkDocumentHardClosedFromAccountingOfficeCloseAsync(SourceType sourceType, Guid sourceId, Guid organizationId, Guid currentUser)
    {
        switch (sourceType)
        {
            case SourceType.InvoicePayment:
                await MarkPaymentHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Deposit:
                await MarkDepositHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Transfer:
                await MarkTransferHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Invoice:
                await MarkInvoiceHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.Bill:
            case SourceType.Receipt:
                await MarkReceiptHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.BillPayment:
                await MarkBillPaymentHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                break;
        }
    }

    private async Task MarkPaymentHardClosedFromAccountingOfficeCloseAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || !CanHardCloseDocumentFromAccountingOfficeClose(payment.PostingStatusId))
            return;

        payment.PostingStatusId = (int)PostingStatus.HardClosed;
        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
    }

    private async Task MarkDepositHardClosedFromAccountingOfficeCloseAsync(Guid depositId, Guid organizationId, Guid currentUser)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null || !CanHardCloseDocumentFromAccountingOfficeClose(deposit.PostingStatusId))
            return;

        deposit.PostingStatusId = (int)PostingStatus.HardClosed;
        deposit.ModifiedBy = currentUser;
        await _accountingRepository.UpdateDepositAsync(deposit);
    }

    private async Task MarkTransferHardClosedFromAccountingOfficeCloseAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
        if (transfer == null || !CanHardCloseDocumentFromAccountingOfficeClose(transfer.PostingStatusId))
            return;

        transfer.PostingStatusId = (int)PostingStatus.HardClosed;
        transfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(transfer);
    }

    private async Task MarkInvoiceHardClosedFromAccountingOfficeCloseAsync(Guid invoiceId, Guid organizationId, Guid currentUser)
    {
        var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, organizationId);
        if (invoice == null || !CanHardCloseDocumentFromAccountingOfficeClose(invoice.PostingStatusId))
            return;

        invoice.PostingStatusId = (int)PostingStatus.HardClosed;
        invoice.ModifiedBy = currentUser;
        await _accountingRepository.UpdateByIdAsync(invoice);
    }

    private async Task MarkReceiptHardClosedFromAccountingOfficeCloseAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, organizationId);
        if (receipt == null || !CanHardCloseDocumentFromAccountingOfficeClose(receipt.PostingStatusId))
            return;

        receipt.PostingStatusId = (int)PostingStatus.HardClosed;
        receipt.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateReceiptAsync(receipt);
    }

    private async Task MarkBillPaymentHardClosedFromAccountingOfficeCloseAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        await MarkReceiptHardClosedFromAccountingOfficeCloseAsync(receiptId, organizationId, currentUser);
        var allocations = await _accountingRepository.GetBillAllocationsByReceiptIdAsync(receiptId, organizationId);
        foreach (var paymentId in allocations.Select(allocation => allocation.PaymentId).Distinct())
        {
            if (paymentId == Guid.Empty)
                continue;

            await MarkPaymentHardClosedFromAccountingOfficeCloseAsync(paymentId, organizationId, currentUser);
        }
    }

    private async Task PostOpenJournalEntriesForSourceAsync(Guid organizationId, int officeId, SourceType sourceType, Guid sourceId, Guid currentOrganizationId, Guid currentUser)
    {
        var journalEntries = await GetJournalEntriesForSourceAsync(organizationId, officeId, sourceType, sourceId);
        foreach (var journalEntry in journalEntries.Where(entry => entry.PostingStatusId == PostingStatus.Open))
            await PostJournalEntryAsync(journalEntry.JournalEntryId, currentOrganizationId, currentUser);
    }

    private sealed class ClearedReconcileLineTargets
    {
        public HashSet<(SourceType SourceType, Guid SourceId)> SourceKeys { get; } = new();
        public HashSet<Guid> DirectPostJournalEntryIds { get; } = new();
        public HashSet<Guid> AllJournalEntryIds { get; } = new();
    }
    #endregion
}

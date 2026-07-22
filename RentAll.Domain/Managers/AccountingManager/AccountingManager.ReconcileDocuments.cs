using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task ApplyDocumentPostingStatusFromReconcileAsync(
        CompleteReconcileRequest request,
        Guid organizationId,
        Guid currentUser)
    {
        if (request.Lines.Count == 0)
            return;

        var clearedLineIds = request.Lines
            .Where(line => line.IsCleared && line.JournalEntryLineId != Guid.Empty)
            .Select(line => line.JournalEntryLineId)
            .Distinct()
            .ToList();
        if (clearedLineIds.Count == 0)
            return;

        var sourceKeys = new HashSet<(SourceType SourceType, Guid SourceId)>();
        foreach (var lineId in clearedLineIds)
        {
            var line = await _journalEntryRepository.GetJournalEntryLineByIdAsync(lineId);
            if (line == null || line.JournalEntryId == Guid.Empty)
                continue;

            var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(line.JournalEntryId, organizationId);
            if (journalEntry?.SourceId is not { } sourceId || sourceId == Guid.Empty)
                continue;

            sourceKeys.Add(((SourceType)journalEntry.SourceTypeId, sourceId));
        }

        foreach (var (sourceType, sourceId) in sourceKeys)
            await MarkDocumentPostedFromReconcileAsync(sourceType, sourceId, organizationId, currentUser);
    }

    private async Task MarkDocumentPostedFromReconcileAsync(
        SourceType sourceType,
        Guid sourceId,
        Guid organizationId,
        Guid currentUser)
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
        await PostOpenJournalEntriesForSourceAsync(payment.OrganizationId, payment.OfficeId, SourceType.InvoicePayment, paymentId, organizationId, currentUser);
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

    private static bool CanMarkDocumentPostedFromReconcile(int? postingStatusId)
    {
        var postingStatus = postingStatusId is >= 0 and <= (int)PostingStatus.HardClosed
            ? (PostingStatus)postingStatusId.Value
            : PostingStatus.Open;

        return postingStatus == PostingStatus.Open;
    }

    private async Task PostOpenJournalEntriesForSourceAsync(
        Guid organizationId,
        int officeId,
        SourceType sourceType,
        Guid sourceId,
        Guid currentOrganizationId,
        Guid currentUser)
    {
        var journalEntries = await GetJournalEntriesForSourceAsync(organizationId, officeId, sourceType, sourceId);
        foreach (var journalEntry in journalEntries.Where(entry => entry.PostingStatusId == PostingStatus.Open))
            await PostJournalEntryAsync(journalEntry.JournalEntryId, currentOrganizationId, currentUser);
    }
}

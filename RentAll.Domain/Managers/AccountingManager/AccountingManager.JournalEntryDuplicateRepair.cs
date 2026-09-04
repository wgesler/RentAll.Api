using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task ReconnectDuplicateJournalEntryLinksAsync(
        JournalEntry retainedEntry,
        JournalEntry duplicateEntry,
        Guid organizationId,
        Guid currentUser)
    {
        if (currentUser == Guid.Empty)
            return;

        var retained = await _journalEntryRepository.GetJournalEntryByIdAsync(retainedEntry.JournalEntryId, organizationId)
            ?? retainedEntry;
        var duplicate = await _journalEntryRepository.GetJournalEntryByIdAsync(duplicateEntry.JournalEntryId, organizationId)
            ?? duplicateEntry;

        await MergeJournalEntryDocumentLinksOntoRetainedAsync(retained, duplicate, currentUser);

        var lineMap = BuildDuplicateToRetainedJournalEntryLineMap(
            retained.JournalEntryLines ?? [],
            duplicate.JournalEntryLines ?? []);

        if (lineMap.Count == 0)
            return;

        await RepointDepositTransferSplitJournalEntryLineIdsAsync(
            organizationId,
            retained.OfficeId,
            lineMap,
            currentUser);
    }

    private async Task MergeJournalEntryDocumentLinksOntoRetainedAsync(JournalEntry retained, JournalEntry duplicate, Guid currentUser)
    {
        var changed = false;

        if ((retained.PaymentId is null || retained.PaymentId == Guid.Empty)
            && duplicate.PaymentId is { } paymentId && paymentId != Guid.Empty)
        {
            retained.PaymentId = paymentId;
            retained.PaymentCode = duplicate.PaymentCode;
            changed = true;
        }

        if ((retained.DepositId is null || retained.DepositId == Guid.Empty)
            && duplicate.DepositId is { } depositId && depositId != Guid.Empty)
        {
            retained.DepositId = depositId;
            retained.DepositCode = duplicate.DepositCode;
            changed = true;
        }

        if ((retained.TransferId is null || retained.TransferId == Guid.Empty)
            && duplicate.TransferId is { } transferId && transferId != Guid.Empty)
        {
            retained.TransferId = transferId;
            retained.TransferCode = duplicate.TransferCode;
            changed = true;
        }

        if (!changed)
            return;

        retained.ModifiedBy = currentUser;
        await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(retained, requireActiveLines: true);
    }

    private static Dictionary<Guid, Guid> BuildDuplicateToRetainedJournalEntryLineMap(
        IReadOnlyList<JournalEntryLine> retainedLines,
        IReadOnlyList<JournalEntryLine> duplicateLines)
    {
        var map = new Dictionary<Guid, Guid>();
        var availableRetained = retainedLines.ToList();

        foreach (var duplicateLine in duplicateLines)
        {
            if (duplicateLine.JournalEntryLineId == Guid.Empty)
                continue;

            var match = availableRetained.FirstOrDefault(retained =>
                retained.ChartOfAccountId == duplicateLine.ChartOfAccountId
                && retained.Debit == duplicateLine.Debit
                && retained.Credit == duplicateLine.Credit
                && retained.PropertyId == duplicateLine.PropertyId
                && retained.ReservationId == duplicateLine.ReservationId
                && retained.ContactId == duplicateLine.ContactId
                && retained.PerspectiveId == duplicateLine.PerspectiveId);

            if (match == null || match.JournalEntryLineId == Guid.Empty)
                continue;

            map[duplicateLine.JournalEntryLineId] = match.JournalEntryLineId;
            availableRetained.Remove(match);
        }

        return map;
    }

    private async Task RepointDepositTransferSplitJournalEntryLineIdsAsync(
        Guid organizationId,
        int officeId,
        IReadOnlyDictionary<Guid, Guid> lineMap,
        Guid currentUser)
    {
        if (lineMap.Count == 0)
            return;

        var duplicateLineIds = lineMap.Keys.ToHashSet();
        var officeIds = officeId > 0 ? officeId.ToString() : string.Empty;

        var deposits = _officeSyncCache?.Deposits?.ToList()
            ?? (await _accountingRepository.GetDepositsByOfficeIdsAsync(organizationId, officeIds)).ToList();

        foreach (var deposit in deposits)
        {
            var changed = false;
            foreach (var split in deposit.Splits ?? [])
            {
                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty || !duplicateLineIds.Contains(lineId))
                    continue;

                if (!lineMap.TryGetValue(lineId, out var retainedLineId))
                    continue;

                split.JournalEntryLineId = retainedLineId;
                changed = true;
            }

            if (!changed)
                continue;

            deposit.ModifiedBy = currentUser;
            await _accountingRepository.UpdateDepositAsync(deposit);
        }

        var transfers = _officeSyncCache?.Transfers?.ToList()
            ?? (await _accountingRepository.GetTransfersByOfficeIdsAsync(organizationId, officeIds)).ToList();

        foreach (var transfer in transfers)
        {
            var changed = false;
            foreach (var split in transfer.Splits ?? [])
            {
                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty || !duplicateLineIds.Contains(lineId))
                    continue;

                if (!lineMap.TryGetValue(lineId, out var retainedLineId))
                    continue;

                split.JournalEntryLineId = retainedLineId;
                changed = true;
            }

            if (!changed)
                continue;

            transfer.ModifiedBy = currentUser;
            await _accountingRepository.UpdateTransferAsync(transfer);
        }
    }

    private static void SortJournalEntriesForDuplicateRetention(List<JournalEntry> entries)
    {
        entries.Sort(static (left, right) =>
        {
            var codeCompare = string.Compare(left.JournalEntryCode, right.JournalEntryCode, StringComparison.OrdinalIgnoreCase);
            return codeCompare != 0 ? codeCompare : left.JournalEntryId.CompareTo(right.JournalEntryId);
        });
    }
}

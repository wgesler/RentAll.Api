using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<Deposit> PrepareDepositForSaveAsync(Deposit deposit)
    {
        await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit);
        await EnrichDepositSplitsFromJournalEntryLinesAsync(deposit);
        ApplyDepositHeaderContextFromSplits(deposit);
        return deposit;
    }

    public async Task<Transfer> PrepareTransferForSaveAsync(Transfer transfer)
    {
        await ReconcileTransferSplitJournalEntryLineIdsAsync(transfer);
        await EnrichTransferSplitsFromJournalEntryLinesAsync(transfer);
        ApplyTransferHeaderContextFromSplits(transfer);
        return transfer;
    }

    private async Task EnrichDepositSplitsFromJournalEntryLinesAsync(Deposit deposit)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0)
            return;

        var sourceLineIds = deposit.Splits
            .Where(split => split.JournalEntryLineId.HasValue && split.JournalEntryLineId != Guid.Empty)
            .Select(split => split.JournalEntryLineId!.Value)
            .Distinct()
            .ToList();

        if (sourceLineIds.Count == 0)
            return;

        var sourceLines = await LoadJournalEntryLinesByIdsAsync(sourceLineIds);

        foreach (var split in deposit.Splits)
        {
            if (!split.JournalEntryLineId.HasValue || split.JournalEntryLineId == Guid.Empty)
                continue;

            if (!sourceLines.TryGetValue(split.JournalEntryLineId.Value, out var sourceLine))
                continue;

            ApplyJournalEntryLineContextToDepositSplit(split, sourceLine);
        }
    }

    private async Task EnrichTransferSplitsFromJournalEntryLinesAsync(Transfer transfer)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0)
            return;

        var sourceLineIds = transfer.Splits
            .Where(split => split.JournalEntryLineId.HasValue && split.JournalEntryLineId != Guid.Empty)
            .Select(split => split.JournalEntryLineId!.Value)
            .Distinct()
            .ToList();

        if (sourceLineIds.Count == 0)
            return;

        var sourceLines = await LoadJournalEntryLinesByIdsAsync(sourceLineIds);

        foreach (var split in transfer.Splits)
        {
            if (!split.JournalEntryLineId.HasValue || split.JournalEntryLineId == Guid.Empty)
                continue;

            if (!sourceLines.TryGetValue(split.JournalEntryLineId.Value, out var sourceLine))
                continue;

            ApplyJournalEntryLineContextToTransferSplit(split, sourceLine);
        }
    }

    private async Task<Dictionary<Guid, JournalEntryLine>> LoadJournalEntryLinesByIdsAsync(IEnumerable<Guid> journalEntryLineIds)
    {
        var sourceLines = new Dictionary<Guid, JournalEntryLine>();

        foreach (var journalEntryLineId in journalEntryLineIds)
        {
            if (journalEntryLineId == Guid.Empty || sourceLines.ContainsKey(journalEntryLineId))
                continue;

            var sourceLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
            if (sourceLine != null)
                sourceLines[journalEntryLineId] = sourceLine;
        }

        return sourceLines;
    }

    private static void ApplyJournalEntryLineContextToDepositSplit(DepositSplit split, JournalEntryLine sourceLine)
    {
        split.PropertyId = NormalizeOptionalGuid(sourceLine.PropertyId);
        split.ReservationId = NormalizeOptionalGuid(sourceLine.ReservationId);
        split.ContactId = NormalizeOptionalGuid(sourceLine.ContactId);
    }

    private static void ApplyJournalEntryLineContextToTransferSplit(TransferSplit split, JournalEntryLine sourceLine)
    {
        split.PropertyId = NormalizeOptionalGuid(sourceLine.PropertyId);
        split.ReservationId = NormalizeOptionalGuid(sourceLine.ReservationId);
        split.ContactId = NormalizeOptionalGuid(sourceLine.ContactId);
        split.SourceJournalEntryLineAmount = Math.Abs(sourceLine.Debit - sourceLine.Credit);
    }

    public Task EnrichTransferSplitsForDisplayAsync(Transfer transfer)
        => EnrichTransferSplitsFromJournalEntryLinesAsync(transfer);

    private static void ApplyDepositHeaderContextFromSplits(Deposit deposit)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0)
            return;

        deposit.PropertyId ??= FirstSplitContextId(deposit.Splits, split => split.PropertyId);
    }

    private static void ApplyTransferHeaderContextFromSplits(Transfer transfer)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0)
            return;

        transfer.PropertyId ??= FirstSplitContextId(transfer.Splits, split => split.PropertyId);
    }

    private static Guid? FirstSplitContextId<TSplit>(IEnumerable<TSplit> splits, Func<TSplit, Guid?> selector)
        => splits
            .Select(selector)
            .FirstOrDefault(id => id.HasValue && id != Guid.Empty);

    private static Guid? NormalizeOptionalGuid(Guid? value)
        => value is { } id && id != Guid.Empty ? id : null;
}

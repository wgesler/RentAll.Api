using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private sealed class EscrowDepositLineCandidate
    {
        public Guid JournalEntryLineId { get; init; }
        public decimal NetAmount { get; init; }
        public Guid? PropertyId { get; init; }
        public Guid? ReservationId { get; init; }
        public Guid? ContactId { get; init; }
        public DateOnly TransactionDate { get; init; }
    }

    private async Task ReconcileTransferSplitJournalEntryLineIdsAsync(Transfer transfer)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0 || transfer.OfficeId <= 0)
            return;

        if (!await IsAccountingFeatureEnabledAsync(transfer.OrganizationId))
            return;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = transfer.BankAccountId is > 0
            ? transfer.BankAccountId.Value
            : GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
            return;

        var escrowLineCandidates = await BuildEscrowDepositLineCandidatesAsync(transfer, escrowDepositAccountId);
        if (escrowLineCandidates.Count == 0)
            return;

        var claimedLineIds = await GetJournalEntryLineIdsClaimedByOtherTransfersAsync(transfer);
        var assignedLineIds = new HashSet<Guid>();

        foreach (var splitGroup in GroupTransferSplitsForReconciliation(transfer.Splits))
        {
            var referenceLineId = splitGroup
                .Select(split => split.JournalEntryLineId)
                .FirstOrDefault(id => id is { } lineId && lineId != Guid.Empty);

            if (referenceLineId is { } validLineId
                && validLineId != Guid.Empty
                && await IsValidTransferSplitGroupJournalEntryLineAsync(splitGroup, validLineId, escrowDepositAccountId))
            {
                assignedLineIds.Add(validLineId);
                continue;
            }

            var resolvedLineId = ResolveTransferSplitGroupJournalEntryLineId(
                transfer,
                splitGroup,
                escrowLineCandidates,
                claimedLineIds,
                assignedLineIds);

            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                foreach (var split in splitGroup)
                    split.JournalEntryLineId = resolvedLineId;

                assignedLineIds.Add(resolvedLineId.Value);
            }
        }
    }

    private static IEnumerable<List<TransferSplit>> GroupTransferSplitsForReconciliation(IReadOnlyList<TransferSplit> splits)
    {
        var groups = new Dictionary<string, List<TransferSplit>>();

        foreach (var split in splits)
        {
            var key = split.JournalEntryLineId is { } lineId && lineId != Guid.Empty
                ? $"line:{lineId}"
                : $"ctx:{NormalizeOptionalGuid(split.PropertyId)}:{NormalizeOptionalGuid(split.ReservationId)}:{NormalizeOptionalGuid(split.ContactId)}";

            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups[key] = group;
            }

            group.Add(split);
        }

        return groups.Values;
    }

    private async Task<List<EscrowDepositLineCandidate>> BuildEscrowDepositLineCandidatesAsync(
        Transfer transfer,
        int escrowDepositAccountId)
    {
        var depositEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = transfer.OrganizationId,
            OfficeIds = transfer.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Deposit,
            IncludeVoided = false,
            IncludeUnposted = true
        })).ToList();

        var candidates = new List<EscrowDepositLineCandidate>();
        foreach (var depositEntry in depositEntries)
        {
            foreach (var line in depositEntry.JournalEntryLines)
            {
                if (line.ChartOfAccountId != escrowDepositAccountId)
                    continue;

                var netAmount = line.Debit - line.Credit;
                if (Math.Abs(netAmount) <= 0.005m)
                    continue;

                candidates.Add(new EscrowDepositLineCandidate
                {
                    JournalEntryLineId = line.JournalEntryLineId,
                    NetAmount = netAmount,
                    PropertyId = NormalizeOptionalGuid(line.PropertyId),
                    ReservationId = NormalizeOptionalGuid(line.ReservationId),
                    ContactId = NormalizeOptionalGuid(line.ContactId),
                    TransactionDate = depositEntry.TransactionDate
                });
            }
        }

        return candidates;
    }

    private async Task<HashSet<Guid>> GetJournalEntryLineIdsClaimedByOtherTransfersAsync(Transfer transfer)
    {
        var claimedLineIds = new HashSet<Guid>();
        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = transfer.OrganizationId,
            OfficeIds = transfer.OfficeId.ToString(),
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        foreach (var otherTransfer in transfers)
        {
            if (otherTransfer.TransferId == transfer.TransferId || otherTransfer.IsActive == false)
                continue;

            foreach (var split in otherTransfer.Splits ?? [])
            {
                if (split.JournalEntryLineId is { } journalEntryLineId && journalEntryLineId != Guid.Empty)
                    claimedLineIds.Add(journalEntryLineId);
            }
        }

        return claimedLineIds;
    }

    private async Task<bool> IsValidTransferSplitGroupJournalEntryLineAsync(
        IReadOnlyList<TransferSplit> splitGroup,
        Guid journalEntryLineId,
        int escrowDepositAccountId)
    {
        var line = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
        if (line == null)
            return false;

        if (line.ChartOfAccountId != escrowDepositAccountId)
            return false;

        return splitGroup.Any(split => TransferSplitContextMatchesLine(split, line));
    }

    private static Guid? ResolveTransferSplitGroupJournalEntryLineId(
        Transfer transfer,
        IReadOnlyList<TransferSplit> splitGroup,
        IReadOnlyList<EscrowDepositLineCandidate> candidates,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        var groupAmount = splitGroup.Sum(split => split.Amount);
        if (Math.Abs(groupAmount) <= 0.005m)
            return null;

        var matches = candidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(candidate.NetAmount - groupAmount) <= 0.005m
                && splitGroup.Any(split => TransferSplitContextMatchesCandidate(split, candidate)))
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0].JournalEntryLineId;

        var transferDate = transfer.TransferDate;
        var closestMatches = matches
            .OrderBy(candidate => Math.Abs(candidate.TransactionDate.DayNumber - transferDate.DayNumber))
            .ThenBy(candidate => candidate.JournalEntryLineId)
            .ToList();

        var bestDistance = Math.Abs(closestMatches[0].TransactionDate.DayNumber - transferDate.DayNumber);
        var tiedMatches = closestMatches
            .Where(candidate => Math.Abs(candidate.TransactionDate.DayNumber - transferDate.DayNumber) == bestDistance)
            .ToList();

        return tiedMatches.Count == 1 ? tiedMatches[0].JournalEntryLineId : null;
    }

    private static bool TransferSplitContextMatchesLine(TransferSplit split, JournalEntryLine line)
        => TransferSplitContextMatches(split, line.PropertyId, line.ReservationId, line.ContactId);

    private static bool TransferSplitContextMatchesCandidate(TransferSplit split, EscrowDepositLineCandidate candidate)
        => TransferSplitContextMatches(split, candidate.PropertyId, candidate.ReservationId, candidate.ContactId);

    private static bool TransferSplitContextMatches(
        TransferSplit split,
        Guid? propertyId,
        Guid? reservationId,
        Guid? contactId)
    {
        if (!TransferSplitGuidMatches(split.PropertyId, propertyId))
            return false;

        if (!TransferSplitGuidMatches(split.ReservationId, reservationId))
            return false;

        return TransferSplitGuidMatches(split.ContactId, contactId);
    }

    private static bool TransferSplitGuidMatches(Guid? expected, Guid? actual)
    {
        var normalizedExpected = NormalizeOptionalGuid(expected);
        var normalizedActual = NormalizeOptionalGuid(actual);
        if (normalizedExpected == null || normalizedActual == null)
            return true;

        return normalizedExpected == normalizedActual;
    }

    private static bool TransferSplitJournalEntryLineIdsChanged(
        IReadOnlyList<Guid?> originalLineIds,
        IReadOnlyList<TransferSplit>? reconciledSplits)
    {
        var currentLineIds = (reconciledSplits ?? [])
            .Select(split => split.JournalEntryLineId)
            .ToList();

        if (originalLineIds.Count != currentLineIds.Count)
            return true;

        for (var index = 0; index < originalLineIds.Count; index++)
        {
            if (originalLineIds[index] != currentLineIds[index])
                return true;
        }

        return false;
    }
}

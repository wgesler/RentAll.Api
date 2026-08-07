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
        public Guid? DepositId { get; init; }
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

            Guid? resolvedLineId = null;
            if (escrowLineCandidates.Count > 0)
            {
                resolvedLineId = ResolveTransferSplitGroupJournalEntryLineId(
                    transfer,
                    splitGroup,
                    escrowLineCandidates,
                    claimedLineIds,
                    assignedLineIds);
            }

            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                foreach (var split in splitGroup)
                    split.JournalEntryLineId = resolvedLineId;

                assignedLineIds.Add(resolvedLineId.Value);
                continue;
            }

            // Stale after clear/resync: clear so callers rematch instead of treating as valid.
            foreach (var split in splitGroup)
            {
                if (split.JournalEntryLineId is { } staleLineId && staleLineId != Guid.Empty)
                    split.JournalEntryLineId = null;
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetUnresolvedTransferSplitMessagesAsync(Transfer transfer)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0 || transfer.OfficeId <= 0)
            return [];

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = transfer.BankAccountId is > 0
            ? transfer.BankAccountId.Value
            : GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
            return [];

        var transferLabel = string.IsNullOrWhiteSpace(transfer.TransferCode)
            ? transfer.TransferId.ToString()
            : transfer.TransferCode.Trim();
        var messages = new List<string>();

        foreach (var splitGroup in GroupTransferSplitsForReconciliation(transfer.Splits))
        {
            var groupAmount = RoundCurrency(splitGroup.Sum(split => split.Amount));
            if (Math.Abs(groupAmount) <= 0.005m)
                continue;

            var referenceLineId = splitGroup
                .Select(split => split.JournalEntryLineId)
                .FirstOrDefault(id => id is { } lineId && lineId != Guid.Empty);

            if (referenceLineId is { } lineId
                && lineId != Guid.Empty
                && await IsValidTransferSplitGroupJournalEntryLineAsync(splitGroup, lineId, escrowDepositAccountId))
            {
                continue;
            }

            messages.Add(
                $"Transfer {transferLabel}: split group amount {groupAmount:0.00} is not linked to a valid escrow deposit journal entry line.");
        }

        return messages;
    }

    private static IEnumerable<List<TransferSplit>> GroupTransferSplitsForReconciliation(IReadOnlyList<TransferSplit> splits)
    {
        var groups = new Dictionary<string, List<TransferSplit>>(StringComparer.OrdinalIgnoreCase);

        foreach (var split in splits)
        {
            // Keep Owner/SD/SDW/Business together: same escrow line, else same description, else context.
            // Description matters after clear/resync clears line ids — context alone can split the group.
            string key;
            if (split.JournalEntryLineId is { } lineId && lineId != Guid.Empty)
                key = $"line:{lineId}";
            else if (!string.IsNullOrWhiteSpace(split.Description))
                key = $"desc:{split.Description.Trim()}";
            else
                key = $"ctx:{NormalizeOptionalGuid(split.PropertyId)}:{NormalizeOptionalGuid(split.ReservationId)}:{NormalizeOptionalGuid(split.ContactId)}";

            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups[key] = group;
            }

            group.Add(split);
        }

        return groups.Values;
    }

    private async Task<List<EscrowDepositLineCandidate>> BuildEscrowDepositLineCandidatesAsync(Transfer transfer, int escrowDepositAccountId)
    {
        var depositEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = transfer.OrganizationId,
            OfficeIds = transfer.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Deposit,
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
                    DepositId = NormalizeOptionalGuid(depositEntry.DepositId),
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

    private async Task<bool> IsValidTransferSplitGroupJournalEntryLineAsync(IReadOnlyList<TransferSplit> splitGroup, Guid journalEntryLineId, int escrowDepositAccountId)
    {
        var line = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
        if (line == null)
            return false;

        if (line.ChartOfAccountId != escrowDepositAccountId)
            return false;

        var groupAmount = Math.Abs(RoundCurrency(splitGroup.Sum(split => split.Amount)));
        var lineAmount = Math.Abs(RoundCurrency(line.Debit - line.Credit));
        return groupAmount <= 0.005m || Math.Abs(groupAmount - lineAmount) <= 0.005m;
    }

    private static Guid? ResolveTransferSplitGroupJournalEntryLineId(Transfer transfer, IReadOnlyList<TransferSplit> splitGroup, IReadOnlyList<EscrowDepositLineCandidate> candidates, IReadOnlySet<Guid> claimedLineIds, IReadOnlySet<Guid> assignedLineIds)
    {
        var groupAmount = Math.Abs(RoundCurrency(splitGroup.Sum(split => split.Amount)));
        if (groupAmount <= 0.005m)
            return null;

        // Amount match among unclaimed lines — always pick the best candidate (never leave ties unresolved).
        var amountMatches = candidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(Math.Abs(candidate.NetAmount) - groupAmount) <= 0.005m)
            .ToList();

        if (amountMatches.Count == 0)
            return null;

        return amountMatches
            .OrderByDescending(candidate => splitGroup.Max(split => ScoreTransferSplitContext(split, candidate.PropertyId, candidate.ReservationId, candidate.ContactId)))
            .ThenBy(candidate => Math.Abs(candidate.TransactionDate.DayNumber - transfer.TransferDate.DayNumber))
            .ThenBy(candidate => candidate.JournalEntryLineId)
            .First()
            .JournalEntryLineId;
    }

    private static int ScoreTransferSplitContext(TransferSplit split, Guid? propertyId, Guid? reservationId, Guid? contactId)
    {
        var score = 0;
        if (TransferSplitGuidEquals(split.PropertyId, propertyId))
            score += 4;
        if (TransferSplitGuidEquals(split.ReservationId, reservationId))
            score += 2;
        if (TransferSplitGuidEquals(split.ContactId, contactId))
            score += 1;
        return score;
    }

    private static bool TransferSplitGuidEquals(Guid? left, Guid? right)
    {
        var normalizedLeft = NormalizeOptionalGuid(left);
        var normalizedRight = NormalizeOptionalGuid(right);
        return normalizedLeft != null && normalizedRight != null && normalizedLeft == normalizedRight;
    }

    private static bool TransferSplitContextMatchesLine(TransferSplit split, JournalEntryLine line)
        => TransferSplitContextMatches(split, line.PropertyId, line.ReservationId, line.ContactId);

    private static bool TransferSplitContextMatchesCandidate(TransferSplit split, EscrowDepositLineCandidate candidate)
        => TransferSplitContextMatches(split, candidate.PropertyId, candidate.ReservationId, candidate.ContactId);

    private static bool TransferSplitContextMatches(TransferSplit split, Guid? propertyId, Guid? reservationId, Guid? contactId)
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

    private static bool TransferSplitJournalEntryLineIdsChanged(IReadOnlyList<Guid?> originalLineIds, IReadOnlyList<TransferSplit>? reconciledSplits)
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

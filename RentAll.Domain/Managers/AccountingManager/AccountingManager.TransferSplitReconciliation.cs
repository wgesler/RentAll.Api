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

    private sealed class TransferDepositInvoiceEscrowMatch
    {
        public Guid DepositId { get; init; }
        public string DepositCode { get; init; } = string.Empty;
        public Guid EscrowJournalEntryLineId { get; init; }
        public decimal EscrowLineAmount { get; init; }
        public string InvoiceSourceCode { get; init; } = string.Empty;
        public Guid? PropertyId { get; init; }
        public decimal DepositSplitAmount { get; init; }
        public DateOnly DepositDate { get; init; }
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

        var invoiceDepositMatches = await BuildTransferDepositInvoiceEscrowMatchesAsync(transfer, escrowDepositAccountId);
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

            // Primary: invoice in transfer description → deposit payment split → deposit escrow JE line.
            Guid? resolvedLineId = ResolveTransferSplitGroupEscrowLineFromDepositInvoice(
                transfer,
                splitGroup,
                invoiceDepositMatches,
                claimedLineIds);

            // Fallback: amount match among escrow deposit lines (non-invoice / legacy).
            if (resolvedLineId is null || resolvedLineId == Guid.Empty)
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

                // Same deposit escrow line may back multiple invoice description groups.
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

        // Multi-invoice transfers often share one deposit escrow line (full deposit amount) while
        // destination splits are grouped by description (per invoice). Pack those groups onto escrow lines.
        PackUnlinkedTransferSplitsOntoEscrowLines(transfer, escrowLineCandidates, claimedLineIds, assignedLineIds);
    }

    private static Guid? ResolveTransferSplitGroupEscrowLineFromDepositInvoice(
        Transfer transfer,
        IReadOnlyList<TransferSplit> splitGroup,
        IReadOnlyList<TransferDepositInvoiceEscrowMatch> invoiceDepositMatches,
        IReadOnlySet<Guid> claimedLineIds)
    {
        var invoiceSourceCode = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup);
        if (string.IsNullOrWhiteSpace(invoiceSourceCode))
            return null;

        var groupAmount = Math.Abs(RoundCurrency(splitGroup.Sum(split => split.Amount)));
        var splitPropertyId = NormalizeOptionalGuid(
            splitGroup.Select(split => split.PropertyId).FirstOrDefault(id => id is { } propertyId && propertyId != Guid.Empty));

        var matches = invoiceDepositMatches
            .Where(match =>
                !claimedLineIds.Contains(match.EscrowJournalEntryLineId)
                && string.Equals(match.InvoiceSourceCode, invoiceSourceCode, StringComparison.OrdinalIgnoreCase)
                && (splitPropertyId == null || TransferSplitGuidEquals(splitPropertyId, match.PropertyId)))
            .ToList();

        if (matches.Count == 0)
            return null;

        // Prefer deposit payment split amount == transfer invoice group total when unique.
        var amountMatches = matches
            .Where(match => Math.Abs(Math.Abs(match.DepositSplitAmount) - groupAmount) <= 0.005m)
            .ToList();

        var ranked = (amountMatches.Count > 0 ? amountMatches : matches)
            .OrderBy(match => Math.Abs(match.DepositDate.DayNumber - transfer.TransferDate.DayNumber))
            .ThenBy(match => match.EscrowJournalEntryLineId)
            .ToList();

        return ranked[0].EscrowJournalEntryLineId;
    }

    private async Task<List<TransferDepositInvoiceEscrowMatch>> BuildTransferDepositInvoiceEscrowMatchesAsync(
        Transfer transfer,
        int escrowDepositAccountId)
    {
        if (_officeSyncCache != null)
        {
            return _officeSyncCache.GetOrBuildTransferInvoiceMatches(
                transfer,
                escrowDepositAccountId,
                TryGetDepositEscrowJournalEntryLineFromCache);
        }

        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = transfer.OrganizationId,
            OfficeIds = transfer.OfficeId.ToString(),
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        var matches = new List<TransferDepositInvoiceEscrowMatch>();
        foreach (var deposit in deposits)
        {
            if (deposit.IsActive == false || deposit.Splits == null || deposit.Splits.Count == 0)
                continue;

            var escrowLine = await TryGetDepositEscrowJournalEntryLineAsync(deposit, escrowDepositAccountId);
            if (escrowLine == null)
                continue;

            var escrowAmount = RoundCurrency(escrowLine.Debit - escrowLine.Credit);
            if (Math.Abs(escrowAmount) <= 0.005m)
                continue;

            foreach (var split in deposit.Splits)
            {
                if (Math.Abs(split.Amount) <= 0.005m)
                    continue;

                var invoiceSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
                if (string.IsNullOrWhiteSpace(invoiceSourceCode))
                    continue;

                matches.Add(new TransferDepositInvoiceEscrowMatch
                {
                    DepositId = deposit.DepositId,
                    DepositCode = deposit.DepositCode ?? string.Empty,
                    EscrowJournalEntryLineId = escrowLine.JournalEntryLineId,
                    EscrowLineAmount = escrowAmount,
                    InvoiceSourceCode = invoiceSourceCode,
                    PropertyId = NormalizeOptionalGuid(split.PropertyId),
                    DepositSplitAmount = RoundCurrency(split.Amount),
                    DepositDate = deposit.DepositDate
                });
            }
        }

        return matches;
    }

    private async Task<JournalEntryLine?> TryGetDepositEscrowJournalEntryLineAsync(Deposit deposit, int escrowDepositAccountId)
    {
        var depositJournalEntries = (await _journalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria
        {
            OrganizationId = deposit.OrganizationId,
            DepositId = deposit.DepositId
        })).ToList();

        foreach (var depositEntry in depositJournalEntries)
        {
            if (depositEntry.SourceTypeId != (int)SourceType.Deposit)
                continue;

            var escrowLine = depositEntry.JournalEntryLines?
                .FirstOrDefault(line => line.ChartOfAccountId == escrowDepositAccountId
                    && Math.Abs(line.Debit - line.Credit) > 0.005m);

            if (escrowLine != null)
                return escrowLine;
        }

        return null;
    }

    private static void PackUnlinkedTransferSplitsOntoEscrowLines(Transfer transfer, IReadOnlyList<EscrowDepositLineCandidate> escrowLineCandidates, IReadOnlySet<Guid> claimedLineIds, HashSet<Guid> assignedLineIds)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0 || escrowLineCandidates.Count == 0)
            return;

        var unlinkedSplits = transfer.Splits
            .Where(split => Math.Abs(split.Amount) > 0.005m
                && (split.JournalEntryLineId is null || split.JournalEntryLineId == Guid.Empty))
            .ToList();

        if (unlinkedSplits.Count == 0)
            return;

        var descriptionGroups = unlinkedSplits
            .Select((split, index) => (split, index))
            .GroupBy(item => string.IsNullOrWhiteSpace(item.split.Description) ? $"split:{item.index}" : item.split.Description.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Select(item => item.split).ToList())
            .Where(group => Math.Abs(group.Sum(split => split.Amount)) > 0.005m)
            .ToList();

        if (descriptionGroups.Count == 0)
            return;

        var remainingGroups = descriptionGroups.ToList();
        var candidates = escrowLineCandidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId))
            .OrderByDescending(candidate => Math.Abs(candidate.NetAmount))
            .ThenBy(candidate => candidate.JournalEntryLineId)
            .ToList();

        foreach (var candidate in candidates)
        {
            if (remainingGroups.Count == 0)
                break;

            var targetAmount = Math.Abs(RoundCurrency(candidate.NetAmount));
            var packed = FindTransferSplitGroupsSummingTo(remainingGroups, targetAmount);
            if (packed == null || packed.Count == 0)
                continue;

            foreach (var group in packed)
            {
                foreach (var split in group)
                    split.JournalEntryLineId = candidate.JournalEntryLineId;

                remainingGroups.Remove(group);
            }

            assignedLineIds.Add(candidate.JournalEntryLineId);
        }
    }

    private static List<List<TransferSplit>>? FindTransferSplitGroupsSummingTo(IReadOnlyList<List<TransferSplit>> groups, decimal targetAmount)
    {
        if (targetAmount <= 0.005m || groups.Count == 0)
            return null;

        var amounts = groups
            .Select(group => Math.Abs(RoundCurrency(group.Sum(split => split.Amount))))
            .ToList();

        // Exact single-group match first.
        for (var index = 0; index < groups.Count; index++)
        {
            if (Math.Abs(amounts[index] - targetAmount) <= 0.005m)
                return [groups[index]];
        }

        // Subset sum for small group counts (typical transfer source packing).
        if (groups.Count > 16)
            return null;

        List<List<TransferSplit>>? best = null;
        void Search(int startIndex, decimal remaining, List<List<TransferSplit>> chosen)
        {
            if (best != null)
                return;

            if (Math.Abs(remaining) <= 0.005m)
            {
                best = chosen.ToList();
                return;
            }

            if (remaining < -0.005m || startIndex >= groups.Count)
                return;

            for (var index = startIndex; index < groups.Count; index++)
            {
                if (amounts[index] - remaining > 0.005m)
                    continue;

                chosen.Add(groups[index]);
                Search(index + 1, RoundCurrency(remaining - amounts[index]), chosen);
                chosen.RemoveAt(chosen.Count - 1);
                if (best != null)
                    return;
            }
        }

        Search(0, targetAmount, []);
        return best;
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

            var invoiceSourceCode = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup) ?? "(missing invoice code)";
            messages.Add(
                $"Transfer {transferLabel}: split group {invoiceSourceCode} amount {groupAmount:0.00} is not linked to a valid escrow deposit journal entry line.");
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

    private async Task<List<EscrowDepositLineCandidate>> BuildEscrowDepositLineCandidatesAsync(
        Transfer transfer,
        int escrowDepositAccountId)
    {
        if (_officeSyncCache != null)
            return _officeSyncCache.GetOrBuildEscrowCandidates(transfer, escrowDepositAccountId);

        var depositEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = transfer.OrganizationId,
            OfficeIds = transfer.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Deposit,
            // Rematch must see deposit JEs before the accounting-office start date.
            StartDate = DateOnly.MinValue,
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
        if (_officeSyncCache != null)
            return _officeSyncCache.GetClaimedTransferLineIdsExcluding(transfer.TransferId);

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
        var line = await GetJournalEntryLineByIdCachedAsync(journalEntryLineId);

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

    private static string? ResolveTransferSplitGroupInvoiceSourceCode(IReadOnlyList<TransferSplit> splitGroup)
    {
        foreach (var split in splitGroup)
        {
            var invoiceSourceCode = ResolveTransferSplitInvoiceSourceCode(split);
            if (!string.IsNullOrWhiteSpace(invoiceSourceCode))
                return invoiceSourceCode;
        }

        return null;
    }

    private static string? ResolveTransferSplitInvoiceSourceCode(TransferSplit split)
    {
        var description = (split.Description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
            return null;

        if (TryParseInvoiceSourceCodeFromMemo(description, out var invoiceSourceCode))
            return invoiceSourceCode;

        var documentSourceCode = TryParseDocumentSourceCodeFromMemo(description);
        if (!string.IsNullOrWhiteSpace(documentSourceCode)
            && documentSourceCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase))
            return documentSourceCode;

        // "Transfer to Escrow Accounts - R-000378-004"
        var separatorIndex = description.LastIndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            var tail = description[(separatorIndex + 3)..].Trim();
            if (!string.IsNullOrWhiteSpace(tail))
                return tail;
        }

        return null;
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

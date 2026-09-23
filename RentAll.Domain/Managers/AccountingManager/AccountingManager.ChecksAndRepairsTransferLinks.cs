using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Checks And Repairs Transfer Links
    private sealed class EscrowDepositLineCandidate
    {
        public Guid JournalEntryLineId { get; init; }
        public decimal NetAmount { get; init; }
        public Guid? PropertyId { get; init; }
        public Guid? ReservationId { get; init; }
        public Guid? ContactId { get; init; }
        public Guid? DepositId { get; init; }
        public DateOnly TransactionDate { get; init; }
        public DateOnly AccountingPeriod { get; init; }
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
        public DateOnly DepositAccountingPeriod { get; init; }
    }

    private Task ReconcileTransferSplitJournalEntryLineIdsAsync(Transfer transfer)
        => ReconcileTransferSplitJournalEntryLineIdsAsync(transfer, trail: null);

    private async Task ReconcileTransferSplitJournalEntryLineIdsAsync(Transfer transfer, AccountingSyncBailTrail? trail)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0 || transfer.OfficeId <= 0)
        {
            trail?.Bail("Rematch exit: no splits or OfficeId missing.");
            return;
        }

        if (!await IsAccountingFeatureEnabledAsync(transfer.OrganizationId))
        {
            trail?.Bail("Rematch exit: accounting feature disabled.");
            return;
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = transfer.BankAccountId is > 0
            ? transfer.BankAccountId.Value
            : GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
        {
            trail?.Bail("Rematch exit: escrow deposit account missing.");
            return;
        }

        var invoiceDepositMatches = await BuildTransferDepositInvoiceEscrowMatchesAsync(transfer, escrowDepositAccountId);
        var escrowLineCandidates = (await BuildEscrowDepositLineCandidatesAsync(transfer, escrowDepositAccountId))
            .Where(candidate =>
                DepositAccountingMonthIsOnOrBeforeTransfer(candidate.TransactionDate, transfer.TransferDate))
            .ToList();
        var claimedLineIds = await GetJournalEntryLineIdsClaimedByOtherTransfersAsync(transfer);
        var assignedLineIds = new HashSet<Guid>();
        trail?.Note(
            $"Rematch: escrowAccount={escrowDepositAccountId} invoiceMatches={invoiceDepositMatches.Count} "
            + $"escrowCandidates={escrowLineCandidates.Count} claimedByOthers={claimedLineIds.Count}");

        foreach (var splitGroup in GroupTransferSplitsForReconciliation(transfer.Splits))
        {
            var groupLabel = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup)
                ?? string.Join(",", splitGroup.Select(split => split.TransferSplitId));
            var groupAmount = RoundCurrency(splitGroup.Sum(split => split.Amount));

            var referenceLineId = splitGroup
                .Select(split => split.JournalEntryLineId)
                .FirstOrDefault(id => id is { } lineId && lineId != Guid.Empty);

            if (referenceLineId is { } validLineId
                && validLineId != Guid.Empty
                && await IsValidTransferSplitGroupJournalEntryLineAsync(transfer, splitGroup, validLineId, escrowDepositAccountId))
            {
                assignedLineIds.Add(validLineId);
                trail?.Note($"Rematch keep: {groupLabel} amount={groupAmount:0.00} line={validLineId}");
                continue;
            }

            // Primary: invoice in transfer description → deposit payment split → deposit escrow JE line.
            Guid? resolvedLineId = ResolveTransferSplitGroupEscrowLineFromDepositInvoice(
                transfer,
                splitGroup,
                invoiceDepositMatches,
                claimedLineIds);

            // Payment code in description (PY-xxx) → stamped deposit → deposit escrow JE line.
            if (resolvedLineId is null || resolvedLineId == Guid.Empty)
            {
                resolvedLineId = await ResolveTransferSplitGroupEscrowLineFromDepositedPaymentAsync(
                    transfer,
                    splitGroup,
                    escrowDepositAccountId,
                    claimedLineIds,
                    assignedLineIds);
            }

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
                trail?.Note($"Rematch linked: {groupLabel} amount={groupAmount:0.00} -> line={resolvedLineId}");
                continue;
            }

            trail?.Bail($"Rematch failed: {groupLabel} amount={groupAmount:0.00} (no escrow deposit line).");
            foreach (var split in splitGroup)
                split.JournalEntryLineId = null;
        }

        // Multi-invoice transfers often share one deposit escrow line (full deposit amount) while
        // destination splits are grouped by description (per invoice). Pack those groups onto escrow lines.
        PackUnlinkedTransferSplitsOntoEscrowLines(transfer, escrowLineCandidates, claimedLineIds, assignedLineIds);
        trail?.Note("Rematch: PackUnlinkedTransferSplitsOntoEscrowLines completed.");
    }

    private static Guid? ResolveTransferSplitGroupEscrowLineFromDepositInvoice(Transfer transfer, IReadOnlyList<TransferSplit> splitGroup, IReadOnlyList<TransferDepositInvoiceEscrowMatch> invoiceDepositMatches, IReadOnlySet<Guid> claimedLineIds)
    {
        var invoiceSourceCode = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup);
        if (string.IsNullOrWhiteSpace(invoiceSourceCode))
            return null;

        var groupAmount = RoundCurrency(splitGroup.Sum(split => split.Amount));
        var splitPropertyId = NormalizeOptionalGuid(
            splitGroup.Select(split => split.PropertyId).FirstOrDefault(id => id is { } propertyId && propertyId != Guid.Empty));

        var matches = invoiceDepositMatches
            .Where(match =>
                !claimedLineIds.Contains(match.EscrowJournalEntryLineId)
                && EntityCodeFormatting.CodesMatch(match.InvoiceSourceCode, invoiceSourceCode)
                && DepositAccountingMonthIsOnOrBeforeTransfer(match.DepositDate, transfer.TransferDate)
                && (splitPropertyId == null || TransferSplitGuidEquals(splitPropertyId, match.PropertyId)))
            .ToList();

        if (matches.Count == 0)
            return null;

        var amountMatches = matches
            .Where(match =>
                Math.Abs(match.DepositSplitAmount - groupAmount) <= 0.005m
                && Math.Abs(match.EscrowLineAmount - groupAmount) <= 0.005m)
            .OrderBy(match => Math.Abs(match.DepositDate.DayNumber - transfer.TransferDate.DayNumber))
            .ThenBy(match => match.EscrowJournalEntryLineId)
            .ToList();

        return amountMatches.Count == 0 ? null : amountMatches[0].EscrowJournalEntryLineId;
    }

    private async Task<Guid?> ResolveTransferSplitGroupEscrowLineFromDepositedPaymentAsync(Transfer transfer, IReadOnlyList<TransferSplit> splitGroup, int escrowDepositAccountId, IReadOnlySet<Guid> claimedLineIds, IReadOnlySet<Guid> assignedLineIds)
    {
        var paymentSourceCode = ResolveTransferSplitGroupPaymentSourceCode(splitGroup);
        if (string.IsNullOrWhiteSpace(paymentSourceCode))
            return null;

        var payments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(
            transfer.OrganizationId,
            transfer.OfficeId.ToString(),
            (int)PaymentKind.Invoice)).ToList();

        var payment = payments
            .Where(row =>
                row.IsActive
                && row.DepositId is { } depositId
                && depositId != Guid.Empty
                && EntityCodeFormatting.CodesMatch(row.PaymentCode, paymentSourceCode))
            .OrderByDescending(row => row.PaymentDate)
            .FirstOrDefault();

        if (payment?.DepositId is not { } linkedDepositId || linkedDepositId == Guid.Empty)
            return null;

        var deposit = await _accountingRepository.GetDepositByIdAsync(linkedDepositId, transfer.OrganizationId);
        if (deposit == null || deposit.IsActive == false)
            return null;

        if (!DepositAccountingMonthIsOnOrBeforeTransfer(deposit.DepositDate, transfer.TransferDate))
            return null;

        var escrowLine = await TryGetDepositEscrowJournalEntryLineAsync(deposit, escrowDepositAccountId);
        if (escrowLine == null)
            return null;

        var groupAmount = RoundCurrency(splitGroup.Sum(split => split.Amount));
        if (Math.Abs(Math.Abs(escrowLine.Debit - escrowLine.Credit) - Math.Abs(groupAmount)) > 0.005m)
            return null;

        if (claimedLineIds.Contains(escrowLine.JournalEntryLineId)
            && !assignedLineIds.Contains(escrowLine.JournalEntryLineId))
            return null;

        return escrowLine.JournalEntryLineId;
    }

    private async Task<List<TransferDepositInvoiceEscrowMatch>> BuildTransferDepositInvoiceEscrowMatchesAsync(Transfer transfer, int escrowDepositAccountId)
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
                if (!IsReservationOrInvoiceSourceCode(invoiceSourceCode))
                {
                    invoiceSourceCode = await ResolvePaymentBackedDepositSplitInvoiceSourceCodeAsync(
                        deposit.OrganizationId,
                        split);
                }

                if (!IsReservationOrInvoiceSourceCode(invoiceSourceCode))
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
                    DepositDate = deposit.DepositDate,
                    DepositAccountingPeriod = deposit.AccountingPeriod
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
            var match = remainingGroups.FirstOrDefault(group =>
                group.Count == 1
                && Math.Abs(Math.Abs(RoundCurrency(group[0].Amount)) - targetAmount) <= 0.005m);
            if (match == null)
                continue;

            match[0].JournalEntryLineId = candidate.JournalEntryLineId;
            remainingGroups.Remove(match);
            assignedLineIds.Add(candidate.JournalEntryLineId);
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
                && await IsValidTransferSplitGroupJournalEntryLineAsync(transfer, splitGroup, lineId, escrowDepositAccountId))
            {
                continue;
            }

            var groupLabel = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup)
                ?? ResolveTransferSplitGroupPaymentSourceCode(splitGroup)
                ?? "(missing source code)";
            messages.Add(
                $"Transfer {transferLabel}: split group {groupLabel} amount {groupAmount:0.00} is not linked to a valid escrow deposit journal entry line.");
        }

        return messages;
    }

    private static IEnumerable<List<TransferSplit>> GroupTransferSplitsForReconciliation(IReadOnlyList<TransferSplit> splits)
    {
        var groups = new Dictionary<string, List<TransferSplit>>(StringComparer.OrdinalIgnoreCase);

        foreach (var split in splits)
        {
            // Keep Owner/SD/SDW/Business together: same payment slice (description), even when rematched to a shared escrow line.
            string key;
            if (split.JournalEntryLineId is { } lineId && lineId != Guid.Empty
                && !string.IsNullOrWhiteSpace(split.Description))
            {
                key = $"line:{lineId}:desc:{split.Description.Trim()}";
            }
            else if (split.JournalEntryLineId is { } lineIdOnly && lineIdOnly != Guid.Empty)
                key = $"line:{lineIdOnly}";
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
            if (depositEntry.DepositId is not { } escrowDepositId || escrowDepositId == Guid.Empty)
                continue;

            var depositDate = await GetDocumentDepositDateAsync(escrowDepositId, transfer.OrganizationId);
            if (depositDate == default)
                continue;

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
                    DepositId = escrowDepositId,
                    TransactionDate = depositDate,
                    AccountingPeriod = depositEntry.AccountingPeriod
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

    private async Task<DateOnly> GetDocumentDepositDateAsync(Guid depositId, Guid organizationId)
    {
        if (depositId == Guid.Empty)
            return default;

        if (_officeSyncCache != null && _officeSyncCache.DepositsById.TryGetValue(depositId, out var cachedDeposit))
            return cachedDeposit.DepositDate;

        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        return deposit?.DepositDate ?? default;
    }

    private async Task<bool> IsValidTransferSplitGroupJournalEntryLineAsync(Transfer transfer, IReadOnlyList<TransferSplit> splitGroup, Guid journalEntryLineId, int escrowDepositAccountId)
    {
        var line = await GetJournalEntryLineByIdCachedAsync(journalEntryLineId);

        if (line == null)
            return false;

        if (line.ChartOfAccountId != escrowDepositAccountId)
            return false;

        var depositJournalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, transfer.OrganizationId);
        if (depositJournalEntry?.DepositId is not { } depositId || depositId == Guid.Empty)
            return false;

        var depositDate = await GetDocumentDepositDateAsync(depositId, transfer.OrganizationId);
        if (!DepositAccountingMonthIsOnOrBeforeTransfer(depositDate, transfer.TransferDate))
            return false;

        var groupAmount = RoundCurrency(splitGroup.Sum(split => split.Amount));
        var lineAmount = RoundCurrency(line.Debit - line.Credit);
        if (Math.Abs(groupAmount) <= 0.005m)
            return false;

        return Math.Abs(groupAmount - lineAmount) <= 0.005m;
    }

    private static Guid? ResolveTransferSplitGroupJournalEntryLineId(Transfer transfer, IReadOnlyList<TransferSplit> splitGroup, IReadOnlyList<EscrowDepositLineCandidate> candidates, IReadOnlySet<Guid> claimedLineIds, IReadOnlySet<Guid> assignedLineIds)
    {
        var groupAmount = RoundCurrency(splitGroup.Sum(split => split.Amount));
        if (Math.Abs(groupAmount) <= 0.005m)
            return null;

        // Amount match among unclaimed lines — always pick the best candidate (never leave ties unresolved).
        var amountMatches = candidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(candidate.NetAmount - groupAmount) <= 0.005m)
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

    private static string? ResolveTransferSplitGroupPaymentSourceCode(IReadOnlyList<TransferSplit> splitGroup)
    {
        foreach (var split in splitGroup)
        {
            var paymentSourceCode = ResolveTransferSplitPaymentSourceCode(split);
            if (!string.IsNullOrWhiteSpace(paymentSourceCode))
                return paymentSourceCode;
        }

        return null;
    }

    private static string? ResolveTransferSplitPaymentSourceCode(TransferSplit split)
    {
        var description = (split.Description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var documentSourceCode = TryParseDocumentSourceCodeFromMemo(description);
        if (!string.IsNullOrWhiteSpace(documentSourceCode)
            && documentSourceCode.StartsWith("PY-", StringComparison.OrdinalIgnoreCase))
            return documentSourceCode;

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
    #endregion
}

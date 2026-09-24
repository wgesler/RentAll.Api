using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Links Check And Repair Transfers
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
        public Guid? ReservationId { get; init; }
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
                DepositTransactionDateIsOnOrBeforeTransfer(candidate.TransactionDate, transfer.TransferDate))
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

            var resolvedLineId = await ResolveTransferSplitGroupEscrowLineStrictAsync(
                transfer,
                splitGroup,
                invoiceDepositMatches,
                escrowLineCandidates,
                escrowDepositAccountId,
                claimedLineIds,
                assignedLineIds);

            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                foreach (var split in splitGroup)
                    split.JournalEntryLineId = resolvedLineId;

                // Same deposit escrow line may back multiple invoice description groups.
                assignedLineIds.Add(resolvedLineId.Value);
                trail?.Note($"Rematch linked: {groupLabel} amount={groupAmount:0.00} -> line={resolvedLineId}");
                continue;
            }

            trail?.Bail($"Rematch failed: {groupLabel} amount={groupAmount:0.00} (no exact escrow line match).");
            foreach (var split in splitGroup)
                split.JournalEntryLineId = null;
        }
    }

    private async Task<Guid?> ResolveTransferSplitGroupEscrowLineStrictAsync(
        Transfer transfer,
        IReadOnlyList<TransferSplit> splitGroup,
        IReadOnlyList<TransferDepositInvoiceEscrowMatch> invoiceDepositMatches,
        IReadOnlyList<EscrowDepositLineCandidate> escrowLineCandidates,
        int escrowDepositAccountId,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        var groupAmount = Math.Abs(RoundCurrency(splitGroup.Sum(split => split.Amount)));
        if (groupAmount <= 0.005m)
            return null;

        var invoiceSourceCode = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup);
        var paymentSourceCode = ResolveTransferSplitGroupPaymentSourceCode(splitGroup);
        var (splitPropertyId, splitReservationId) = ResolveTransferSplitGroupContext(splitGroup);

        var matchingLineIds = new HashSet<Guid>();

        if (!string.IsNullOrWhiteSpace(invoiceSourceCode))
        {
            foreach (var match in invoiceDepositMatches)
            {
                if (claimedLineIds.Contains(match.EscrowJournalEntryLineId))
                    continue;
                if (!EntityCodeFormatting.CodesMatch(match.InvoiceSourceCode, invoiceSourceCode))
                    continue;
                if (!DepositTransactionDateIsOnOrBeforeTransfer(match.DepositDate, transfer.TransferDate))
                    continue;
                if (!SplitLineContextMatches(splitPropertyId, splitReservationId, match.PropertyId, match.ReservationId))
                    continue;
                if (Math.Abs(match.DepositSplitAmount - groupAmount) > 0.005m)
                    continue;

                matchingLineIds.Add(match.EscrowJournalEntryLineId);
            }
        }
        else if (!string.IsNullOrWhiteSpace(paymentSourceCode))
        {
            var paymentLineId = await ResolveTransferSplitGroupEscrowLineFromDepositedPaymentAsync(
                transfer,
                splitGroup,
                escrowDepositAccountId,
                claimedLineIds,
                assignedLineIds);
            if (paymentLineId is { } lineId && lineId != Guid.Empty)
                matchingLineIds.Add(lineId);
        }
        else
        {
            var hints = BuildLinkContextHintsFromTransferSplitGroup(splitGroup);
            foreach (var candidate in escrowLineCandidates)
            {
                if (claimedLineIds.Contains(candidate.JournalEntryLineId)
                    || assignedLineIds.Contains(candidate.JournalEntryLineId))
                    continue;
                if (!DepositTransactionDateIsOnOrBeforeTransfer(candidate.TransactionDate, transfer.TransferDate))
                    continue;
                if (Math.Abs(Math.Abs(candidate.NetAmount) - groupAmount) > 0.005m)
                    continue;

                var line = await GetJournalEntryLineByIdCachedAsync(candidate.JournalEntryLineId);
                if (line == null
                    || !await SplitLineContextMatchesResolvedLineAsync(
                        splitPropertyId,
                        splitReservationId,
                        line,
                        transfer.OrganizationId,
                        hints))
                    continue;

                matchingLineIds.Add(candidate.JournalEntryLineId);
            }
        }

        return matchingLineIds.Count == 1 ? matchingLineIds.First() : null;
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

        if (!DepositTransactionDateIsOnOrBeforeTransfer(deposit.DepositDate, transfer.TransferDate))
            return null;

        var escrowLine = await TryGetDepositEscrowJournalEntryLineAsync(deposit, escrowDepositAccountId);
        if (escrowLine == null)
            return null;

        var groupAmount = Math.Abs(RoundCurrency(splitGroup.Sum(split => split.Amount)));
        if (groupAmount <= 0.005m)
            return null;

        var (splitPropertyId, splitReservationId) = ResolveTransferSplitGroupContext(splitGroup);
        if (!await SplitLineContextMatchesResolvedLineAsync(
                splitPropertyId,
                splitReservationId,
                escrowLine,
                transfer.OrganizationId,
                BuildLinkContextHintsFromTransferSplitGroup(splitGroup)))
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

                var splitContext = await ResolveDepositSplitLinkContextAsync(deposit, split, deposit.OrganizationId);
                matches.Add(new TransferDepositInvoiceEscrowMatch
                {
                    DepositId = deposit.DepositId,
                    DepositCode = deposit.DepositCode ?? string.Empty,
                    EscrowJournalEntryLineId = escrowLine.JournalEntryLineId,
                    EscrowLineAmount = escrowAmount,
                    InvoiceSourceCode = invoiceSourceCode,
                    PropertyId = splitContext.PropertyId,
                    ReservationId = splitContext.ReservationId,
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

    private async Task<bool> UnlinkAmountMismatchedTransferSplitLinksForTransferHealthFixAsync(
        Transfer transfer,
        Guid currentUser,
        AccountingSyncBailTrail? trail)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = transfer.BankAccountId is > 0
            ? transfer.BankAccountId.Value
            : GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
            return false;

        var changed = false;
        var splits = transfer.Splits ?? [];
        var splitsByLine = splits
            .Where(split => split.JournalEntryLineId is { } lineId && lineId != Guid.Empty)
            .GroupBy(split => split.JournalEntryLineId!.Value)
            .ToList();

        foreach (var lineGroup in splitsByLine)
        {
            var lineId = lineGroup.Key;
            var line = await GetJournalEntryLineByIdCachedAsync(lineId);
            if (line == null)
            {
                foreach (var split in lineGroup)
                {
                    trail?.Note($"Step1 unlink: transfer split {split.TransferSplitId} pointed at missing line {lineId}.");
                    split.JournalEntryLineId = null;
                    changed = true;
                }

                continue;
            }

            if (line.ChartOfAccountId != escrowDepositAccountId)
                continue;

            var allocatedAmount = Math.Abs(lineGroup.Sum(split => RoundCurrency(split.Amount)));
            var lineNet = Math.Abs(line.Debit - line.Credit);
            var depositJournalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, transfer.OrganizationId);
            DateOnly depositDate = default;
            if (depositJournalEntry?.DepositId is { } depositId && depositId != Guid.Empty)
                depositDate = await GetDocumentDepositDateAsync(depositId, transfer.OrganizationId);

            var amountMismatch = Math.Abs(lineNet - allocatedAmount) > 0.005m;
            var dateMismatch = depositDate != default
                && !DepositTransactionDateIsOnOrBeforeTransfer(depositDate, transfer.TransferDate);
            if (amountMismatch || dateMismatch)
            {
                foreach (var split in lineGroup)
                {
                    trail?.Note(
                        $"Step1 unlink invalid link: transfer split {split.TransferSplitId} allocated={allocatedAmount:0.00} line={lineId} lineAmount={lineNet:0.00} "
                        + $"amountMismatch={amountMismatch} dateMismatch={dateMismatch}.");
                    split.JournalEntryLineId = null;
                    changed = true;
                }

                continue;
            }

            foreach (var split in lineGroup)
            {
                var contextMismatch = !await SplitLineContextMatchesResolvedLineAsync(
                    split.PropertyId,
                    split.ReservationId,
                    line,
                    transfer.OrganizationId,
                    new JournalEntryLineLinkContextHints
                    {
                        InvoiceSourceCode = ResolveTransferSplitInvoiceSourceCode(split)
                            ?? ResolveTransferSplitPaymentSourceCode(split),
                        TargetAmount = split.Amount
                    });
                if (contextMismatch)
                {
                    trail?.Note(
                        $"Step1 unlink invalid link: transfer split {split.TransferSplitId} amount={Math.Abs(RoundCurrency(split.Amount)):0.00} line={lineId} contextMismatch=true.");
                    split.JournalEntryLineId = null;
                    changed = true;
                }
            }
        }

        if (!changed)
            return false;

        transfer.ModifiedBy = currentUser;
        var updated = await _accountingRepository.UpdateTransferAsync(transfer);
        transfer.Splits = updated.Splits;
        return true;
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
        if (!DepositTransactionDateIsOnOrBeforeTransfer(depositDate, transfer.TransferDate))
            return false;

        if (!TransferSplitLineAllocationMatches(transfer.Splits ?? [], journalEntryLineId, line))
            return false;

        var (splitPropertyId, splitReservationId) = ResolveTransferSplitGroupContext(splitGroup);
        return await SplitLineContextMatchesResolvedLineAsync(
            splitPropertyId,
            splitReservationId,
            line,
            transfer.OrganizationId,
            BuildLinkContextHintsFromTransferSplitGroup(splitGroup));
    }

    private static (Guid? PropertyId, Guid? ReservationId) ResolveTransferSplitGroupContext(IReadOnlyList<TransferSplit> splitGroup)
    {
        var propertyId = NormalizeOptionalGuid(
            splitGroup.Select(split => split.PropertyId).FirstOrDefault(id => id is { } value && value != Guid.Empty));
        var reservationId = NormalizeOptionalGuid(
            splitGroup.Select(split => split.ReservationId).FirstOrDefault(id => id is { } value && value != Guid.Empty));
        return (propertyId, reservationId);
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

    private static bool TransferSplitLineAllocationMatches(
        IReadOnlyList<TransferSplit> splits,
        Guid journalEntryLineId,
        JournalEntryLine line)
    {
        var allocatedAmount = Math.Abs(splits
            .Where(split => split.JournalEntryLineId == journalEntryLineId)
            .Sum(split => RoundCurrency(split.Amount)));
        if (allocatedAmount <= 0.005m)
            return false;

        var lineAmount = Math.Abs(RoundCurrency(line.Debit - line.Credit));
        return Math.Abs(allocatedAmount - lineAmount) <= 0.005m;
    }

    private async Task<HashSet<Guid>> CollectDepositIdsToStampForTransferHealthFixAsync(Transfer transfer)
    {
        var depositIds = new HashSet<Guid>();

        foreach (var split in transfer.Splits ?? [])
        {
            var depositId = await TryResolveDepositIdFromTransferSplitLineAsync(split, transfer.OrganizationId);
            if (depositId != Guid.Empty)
                depositIds.Add(depositId);
        }

        if (_officeSyncCache != null)
        {
            foreach (var deposit in _officeSyncCache.Deposits)
            {
                if (deposit.IsActive
                    && deposit.TransferId == transfer.TransferId
                    && DepositMatchesTransferTransactionDate(transfer, deposit))
                    depositIds.Add(deposit.DepositId);
            }
        }
        else
        {
            var deposits = await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
            {
                OrganizationId = transfer.OrganizationId,
                OfficeIds = transfer.OfficeId.ToString(),
                IsActive = true,
                IncludeInactive = false
            });

            foreach (var deposit in deposits.Where(deposit =>
                         deposit.TransferId == transfer.TransferId
                         && DepositMatchesTransferTransactionDate(transfer, deposit)))
                depositIds.Add(deposit.DepositId);
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = transfer.BankAccountId is > 0
            ? transfer.BankAccountId.Value
            : GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
            return await FilterDepositIdsForTransferTransactionDateAsync(transfer, depositIds);

        var invoiceDepositMatches = await BuildTransferDepositInvoiceEscrowMatchesAsync(transfer, escrowDepositAccountId);
        foreach (var splitGroup in GroupTransferSplitsForReconciliation(transfer.Splits ?? []))
        {
            var groupAmount = Math.Abs(RoundCurrency(splitGroup.Sum(split => split.Amount)));
            if (groupAmount <= 0.005m)
                continue;

            var invoiceSourceCode = ResolveTransferSplitGroupInvoiceSourceCode(splitGroup);
            if (!string.IsNullOrWhiteSpace(invoiceSourceCode))
            {
                var matchingDeposits = invoiceDepositMatches
                    .Where(match =>
                        EntityCodeFormatting.CodesMatch(match.InvoiceSourceCode, invoiceSourceCode)
                        && Math.Abs(match.DepositSplitAmount - groupAmount) <= 0.005m
                        && DepositTransactionDateIsOnOrBeforeTransfer(match.DepositDate, transfer.TransferDate))
                    .Select(match => match.DepositId)
                    .Distinct()
                    .ToList();

                if (matchingDeposits.Count == 1)
                    depositIds.Add(matchingDeposits[0]);

                continue;
            }

            var paymentSourceCode = ResolveTransferSplitGroupPaymentSourceCode(splitGroup);
            if (string.IsNullOrWhiteSpace(paymentSourceCode))
                continue;

            var payments = _officeSyncCache != null
                ? _officeSyncCache.Payments
                : (await _accountingRepository.GetPaymentsByOfficeIdsAsync(
                    transfer.OrganizationId,
                    transfer.OfficeId.ToString(),
                    (int)PaymentKind.Invoice)).ToList();

            var payment = payments
                .Where(row =>
                    row.IsActive
                    && row.PaymentKindId == (int)PaymentKind.Invoice
                    && EntityCodeFormatting.CodesMatch(row.PaymentCode, paymentSourceCode)
                    && Math.Abs(Math.Abs(RoundCurrency(row.Amount)) - groupAmount) <= 0.005m
                    && row.DepositId is { } stampedDepositId
                    && stampedDepositId != Guid.Empty)
                .OrderByDescending(row => row.PaymentDate)
                .FirstOrDefault();

            if (payment?.DepositId is { } paymentDepositId && paymentDepositId != Guid.Empty)
            {
                var paymentDepositDate = await GetDocumentDepositDateAsync(paymentDepositId, transfer.OrganizationId);
                if (DepositMatchesTransferTransactionDate(transfer, paymentDepositDate))
                    depositIds.Add(paymentDepositId);
            }
        }

        return await FilterDepositIdsForTransferTransactionDateAsync(transfer, depositIds);
    }

    private async Task<HashSet<Guid>> FilterDepositIdsForTransferTransactionDateAsync(
        Transfer transfer,
        IReadOnlyCollection<Guid> depositIds)
    {
        var filteredDepositIds = new HashSet<Guid>();
        foreach (var depositId in depositIds)
        {
            if (depositId == Guid.Empty)
                continue;

            var depositDate = await GetDocumentDepositDateAsync(depositId, transfer.OrganizationId);
            if (DepositMatchesTransferTransactionDate(transfer, depositDate))
                filteredDepositIds.Add(depositId);
        }

        return filteredDepositIds;
    }

    private async Task ClearTransferDepositDateViolationsForOfficeHealthFixAsync(
        Guid organizationId,
        string officeIds,
        Guid currentUser)
    {
        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        }))
            .OrderBy(transfer => transfer.TransferDate)
            .ThenBy(transfer => transfer.TransferCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var transfer in transfers)
        {
            if (transfer.Splits == null || transfer.Splits.Count == 0)
                continue;

            var originalLineIds = transfer.Splits.Select(split => split.JournalEntryLineId).ToList();
            var transferChanged = false;

            foreach (var split in transfer.Splits)
            {
                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                    continue;

                var depositId = await TryResolveDepositIdFromTransferSplitLineAsync(split, organizationId);
                if (depositId == Guid.Empty)
                    continue;

                var depositDate = await GetDocumentDepositDateAsync(depositId, organizationId);
                if (DepositMatchesTransferTransactionDate(transfer, depositDate))
                    continue;

                split.JournalEntryLineId = null;
                transferChanged = true;
            }

            if (transferChanged && TransferSplitJournalEntryLineIdsChanged(originalLineIds, transfer.Splits))
            {
                transfer.ModifiedBy = currentUser;
                var updated = await _accountingRepository.UpdateTransferAsync(transfer);
                transfer.Splits = updated.Splits;
                _officeSyncCache?.ReplaceTransfer(transfer);
            }

            var stampedDeposits = _officeSyncCache != null
                ? _officeSyncCache.Deposits
                    .Where(deposit => deposit.IsActive && deposit.TransferId == transfer.TransferId)
                    .ToList()
                : (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
                {
                    OrganizationId = organizationId,
                    OfficeIds = transfer.OfficeId.ToString(),
                    IsActive = true,
                    IncludeInactive = false
                }))
                    .Where(deposit => deposit.TransferId == transfer.TransferId)
                    .ToList();

            foreach (var deposit in stampedDeposits)
            {
                if (DepositMatchesTransferTransactionDate(transfer, deposit))
                    continue;

                await _accountingRepository.SetDepositTransferIdAsync(
                    deposit.DepositId,
                    organizationId,
                    null,
                    currentUser);

                if (_officeSyncCache != null
                    && _officeSyncCache.DepositsById.TryGetValue(deposit.DepositId, out var cachedDeposit))
                {
                    cachedDeposit.TransferId = null;
                    cachedDeposit.TransferCode = string.Empty;
                }
            }
        }

        _officeSyncCache?.InvalidateRematchIndexes();
    }

    private async Task<Guid> TryResolveDepositIdFromTransferSplitLineAsync(TransferSplit split, Guid organizationId)
    {
        if (split.JournalEntryLineId is not { } journalEntryLineId || journalEntryLineId == Guid.Empty)
            return Guid.Empty;

        if (_officeSyncCache != null && _officeSyncCache.TryGetDepositIdForLine(journalEntryLineId, out var cachedDepositId))
            return cachedDepositId;

        var sourceLine = await GetJournalEntryLineByIdCachedAsync(journalEntryLineId);
        if (sourceLine == null || sourceLine.JournalEntryId == Guid.Empty)
            return Guid.Empty;

        var depositJournalEntry = await GetJournalEntryByIdCachedAsync(sourceLine.JournalEntryId, organizationId);
        if (depositJournalEntry == null)
            return Guid.Empty;

        var depositId = NormalizeOptionalGuid(depositJournalEntry.DepositId);
        if (depositId == null && depositJournalEntry.SourceTypeId == (int)SourceType.Deposit)
            depositId = NormalizeOptionalGuid(depositJournalEntry.SourceId);

        if (depositId == null || depositId == Guid.Empty)
        {
            var paymentId = NormalizeOptionalGuid(depositJournalEntry.PaymentId);
            if (paymentId is { } linkedPaymentId)
            {
                var payment = await _accountingRepository.GetPaymentByIdAsync(linkedPaymentId, organizationId);
                depositId = NormalizeOptionalGuid(payment?.DepositId);
            }
        }

        return depositId ?? Guid.Empty;
    }

    private async Task TryStampDepositsForTransferHealthFixAsync(Transfer transfer, Guid currentUser)
    {
        var depositIds = await CollectDepositIdsToStampForTransferHealthFixAsync(transfer);
        if (depositIds.Count == 0)
            return;

        await SyncDepositTransferIdsForTransferAsync(transfer, depositIds, currentUser);

        if (_officeSyncCache == null)
            return;

        foreach (var depositId in depositIds)
        {
            if (_officeSyncCache.DepositsById.TryGetValue(depositId, out var cachedDeposit))
            {
                cachedDeposit.TransferId = transfer.TransferId;
                cachedDeposit.TransferCode = transfer.TransferCode;
            }
        }
    }
    #endregion
}

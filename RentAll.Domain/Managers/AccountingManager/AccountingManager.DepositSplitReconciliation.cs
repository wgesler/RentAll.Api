using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private sealed class UndepositedPaymentLineCandidate
    {
        public Guid JournalEntryLineId { get; init; }
        public decimal NetAmount { get; init; }
        public Guid? PropertyId { get; init; }
        public Guid? ReservationId { get; init; }
        public Guid? ContactId { get; init; }
        public Guid? DepositId { get; init; }
        public string SourceCode { get; init; } = string.Empty;
        public DateOnly TransactionDate { get; init; }
    }

    private Task ReconcileDepositSplitJournalEntryLineIdsAsync(Deposit deposit)
        => ReconcileDepositSplitJournalEntryLineIdsAsync(deposit, trail: null);

    private async Task ReconcileDepositSplitJournalEntryLineIdsAsync(Deposit deposit, AccountingSyncBailTrail? trail)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0 || deposit.OfficeId <= 0)
        {
            trail?.Bail("Rematch exit: no splits or OfficeId missing.");
            return;
        }

        if (!await IsAccountingFeatureEnabledAsync(deposit.OrganizationId))
        {
            trail?.Bail("Rematch exit: accounting feature disabled.");
            return;
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
        {
            trail?.Bail("Rematch exit: DefaultUndepFundsAccountId missing.");
            return;
        }

        var paymentLineCandidates = await BuildUndepositedPaymentLineCandidatesAsync(deposit, undepositedFundsAccountId);
        var claimedLineIds = await GetJournalEntryLineIdsClaimedByOtherDepositsAsync(deposit);
        var assignedLineIds = new HashSet<Guid>();
        trail?.Note($"Rematch: UF account={undepositedFundsAccountId} candidates={paymentLineCandidates.Count} claimedByOthers={claimedLineIds.Count}");

        foreach (var split in deposit.Splits)
        {
            var splitLabel = ResolveDepositSplitInvoiceSourceCode(split) ?? split.DepositSplitId.ToString();
            if (await IsValidDepositSplitJournalEntryLineAsync(deposit.OrganizationId, split, undepositedFundsAccountId))
            {
                if (split.JournalEntryLineId.HasValue && split.JournalEntryLineId != Guid.Empty)
                    assignedLineIds.Add(split.JournalEntryLineId.Value);

                trail?.Note($"Rematch keep: {splitLabel} amount={split.Amount:0.00} line={split.JournalEntryLineId}");
                continue;
            }

            Guid? resolvedLineId = null;
            if (paymentLineCandidates.Count > 0)
            {
                resolvedLineId = ResolveDepositSplitJournalEntryLineId(
                    deposit,
                    split,
                    paymentLineCandidates,
                    claimedLineIds,
                    assignedLineIds);
            }

            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                split.JournalEntryLineId = resolvedLineId;
                assignedLineIds.Add(resolvedLineId.Value);
                trail?.Note($"Rematch linked: {splitLabel} amount={split.Amount:0.00} -> line={resolvedLineId}");
                continue;
            }

            resolvedLineId = await ResolveDepositSplitByDepositedPaymentStampAsync(
                deposit,
                split,
                undepositedFundsAccountId,
                claimedLineIds,
                assignedLineIds);
            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                split.JournalEntryLineId = resolvedLineId;
                assignedLineIds.Add(resolvedLineId.Value);
                trail?.Note($"Rematch linked via payment stamp: {splitLabel} amount={split.Amount:0.00} -> line={resolvedLineId}");
                continue;
            }

            resolvedLineId = await ResolveHealthFixDepositSplitLineIdAsync(
                deposit,
                split,
                undepositedFundsAccountId,
                claimedLineIds,
                assignedLineIds);
            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                split.JournalEntryLineId = resolvedLineId;
                assignedLineIds.Add(resolvedLineId.Value);
                trail?.Note($"Rematch linked via invoice payment: {splitLabel} amount={split.Amount:0.00} -> line={resolvedLineId}");
                continue;
            }

            // Stale after clear/resync (or wrong account line): clear so callers rematch instead of treating as valid.
            if (split.JournalEntryLineId is { } staleLineId && staleLineId != Guid.Empty)
            {
                trail?.Bail($"Rematch cleared stale line on {splitLabel} amount={split.Amount:0.00} was={staleLineId}");
                split.JournalEntryLineId = null;
            }
            else
            {
                trail?.Bail($"Rematch failed: {splitLabel} amount={split.Amount:0.00} (no UF payment line).");
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetUnresolvedPaymentBackedDepositSplitMessagesAsync(Deposit deposit)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0 || deposit.OfficeId <= 0)
            return [];

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            return [];

        var depositLabel = string.IsNullOrWhiteSpace(deposit.DepositCode)
            ? deposit.DepositId.ToString()
            : deposit.DepositCode.Trim();
        var messages = new List<string>();

        foreach (var split in deposit.Splits.Where(split => Math.Abs(split.Amount) > 0.005m))
        {
            if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                continue;

            if (await IsValidDepositSplitJournalEntryLineAsync(deposit.OrganizationId, split, undepositedFundsAccountId))
                continue;

            var sourceCode = ResolveDepositSplitInvoiceSourceCode(split) ?? "(missing invoice code)";
            messages.Add(
                $"Deposit {depositLabel}: could not rematch {sourceCode} (${split.Amount:0.00}) to a payment undeposited-funds JE line. If journal entries were cleared, run Sync All first, then Repair (R).");
        }

        return messages;
    }

    private async Task<Guid?> ResolveHealthFixDepositSplitLineIdAsync(
        Deposit deposit,
        DepositSplit split,
        int undepositedFundsAccountId,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
            return null;

        var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        var splitReservationId = NormalizeOptionalGuid(split.ReservationId);
        if (string.IsNullOrWhiteSpace(splitSourceCode) && splitReservationId == null)
            return null;

        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return null;

        var paymentIds = new HashSet<Guid>();
        if (!string.IsNullOrWhiteSpace(splitSourceCode))
        {
            foreach (var paymentId in await FindInvoicePaymentIdsBySourceCodeAsync(
                         deposit.OrganizationId,
                         deposit.OfficeId,
                         splitSourceCode))
            {
                paymentIds.Add(paymentId);
            }
        }

        if (splitReservationId != null)
        {
            foreach (var paymentId in await FindInvoicePaymentIdsByReservationIdAsync(
                         deposit.OrganizationId,
                         deposit.OfficeId,
                         splitReservationId.Value))
            {
                paymentIds.Add(paymentId);
            }
        }

        var matches = new List<(Guid LineId, int Rank)>();
        foreach (var paymentId in paymentIds)
        {
            foreach (var paymentEntry in await GetJournalEntriesByPaymentIdCachedAsync(deposit.OrganizationId, paymentId))
                CollectUndepositedFundsLineMatches(
                    paymentEntry,
                    undepositedFundsAccountId,
                    splitSourceCode ?? string.Empty,
                    splitAmount,
                    claimedLineIds,
                    assignedLineIds,
                    matches);
        }

        var invoiceId = !string.IsNullOrWhiteSpace(splitSourceCode)
            ? await ResolveInvoiceIdBySourceCodeAsync(deposit.OrganizationId, deposit.OfficeId, splitSourceCode)
            : Guid.Empty;
        if (invoiceId != Guid.Empty)
        {
            foreach (var paymentEntry in await GetDocumentJournalEntriesForSyncAsync(
                         deposit.OrganizationId,
                         deposit.OfficeId,
                         SourceType.Invoice,
                         invoiceId,
                         JournalEntryKind.Payment))
            {
                CollectUndepositedFundsLineMatches(
                    paymentEntry,
                    undepositedFundsAccountId,
                    splitSourceCode ?? string.Empty,
                    splitAmount,
                    claimedLineIds,
                    assignedLineIds,
                    matches);
            }
        }

        if (matches.Count == 0)
        {
            matches = await CollectUndepositedFundsLineMatchesByPaymentAmountAsync(
                paymentIds,
                deposit.OrganizationId,
                split,
                undepositedFundsAccountId,
                claimedLineIds,
                assignedLineIds);
        }

        if (matches.Count == 0)
            return null;

        return matches
            .OrderBy(match => match.Rank)
            .ThenBy(match => match.LineId)
            .First()
            .LineId;
    }

    private async Task<List<(Guid LineId, int Rank)>> CollectUndepositedFundsLineMatchesByPaymentAmountAsync(
        IReadOnlyCollection<Guid> paymentIds,
        Guid organizationId,
        DepositSplit split,
        int undepositedFundsAccountId,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        var matches = new List<(Guid LineId, int Rank)>();
        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return matches;

        foreach (var paymentId in paymentIds)
        {
            foreach (var paymentEntry in await GetJournalEntriesByPaymentIdCachedAsync(organizationId, paymentId))
            {
                if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                    continue;

                foreach (var line in paymentEntry.JournalEntryLines ?? [])
                {
                    if (line.ChartOfAccountId != undepositedFundsAccountId)
                        continue;

                    var netAmount = Math.Abs(line.Debit - line.Credit);
                    if (netAmount <= 0.005m || line.JournalEntryLineId == Guid.Empty)
                        continue;

                    if (claimedLineIds.Contains(line.JournalEntryLineId) || assignedLineIds.Contains(line.JournalEntryLineId))
                        continue;

                    if (Math.Abs(netAmount - splitAmount) > 0.005m)
                        continue;

                    matches.Add((line.JournalEntryLineId, -20));
                }
            }
        }

        return matches;
    }

    private static void CollectUndepositedFundsLineMatches(
        JournalEntry paymentEntry,
        int undepositedFundsAccountId,
        string splitSourceCode,
        decimal splitAmount,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds,
        ICollection<(Guid LineId, int Rank)> matches)
    {
        if (paymentEntry.JournalEntryKindId is not (JournalEntryKind.Payment or JournalEntryKind.PrePaymentReceive))
            return;

        var paymentSourceCode = ResolvePaymentJournalEntrySourceCode(paymentEntry);
        var sourceMatches = string.Equals(paymentSourceCode, splitSourceCode, StringComparison.OrdinalIgnoreCase);

        foreach (var line in paymentEntry.JournalEntryLines ?? [])
        {
            if (line.ChartOfAccountId != undepositedFundsAccountId)
                continue;

            var netAmount = Math.Abs(line.Debit - line.Credit);
            if (netAmount <= 0.005m || line.JournalEntryLineId == Guid.Empty)
                continue;

            if (claimedLineIds.Contains(line.JournalEntryLineId) || assignedLineIds.Contains(line.JournalEntryLineId))
                continue;

            var rank = 0;
            if (sourceMatches)
                rank -= 5;

            if (Math.Abs(netAmount - splitAmount) <= 0.005m)
                rank -= 10;

            matches.Add((line.JournalEntryLineId, rank));
        }
    }

    private async Task<Guid> ResolveInvoiceIdBySourceCodeAsync(Guid organizationId, int officeId, string invoiceSourceCode)
    {
        var normalizedSourceCode = invoiceSourceCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSourceCode))
            return Guid.Empty;

        if (_officeSyncCache != null)
        {
            var cachedInvoice = _officeSyncCache.InvoicesById.Values.FirstOrDefault(invoice =>
                string.Equals(invoice.InvoiceCode, normalizedSourceCode, StringComparison.OrdinalIgnoreCase));
            return cachedInvoice?.InvoiceId ?? Guid.Empty;
        }

        var invoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            IncludeInactive = true,
            IncludePaid = true
        });

        return invoices
            .FirstOrDefault(invoice => string.Equals(invoice.InvoiceCode, normalizedSourceCode, StringComparison.OrdinalIgnoreCase))
            ?.InvoiceId ?? Guid.Empty;
    }

    private static bool IsRematchableHealthInvoicePaymentJournalEntry(JournalEntry entry)
        => entry.PaymentId is { } paymentId
            && paymentId != Guid.Empty
            && entry.JournalEntryKindId is JournalEntryKind.Payment or JournalEntryKind.PrePaymentReceive;

    private async Task<List<UndepositedPaymentLineCandidate>> BuildUndepositedPaymentLineCandidatesAsync(Deposit deposit, int undepositedFundsAccountId)
    {
        if (_officeSyncCache != null)
            return _officeSyncCache.GetOrBuildUndepositedCandidates(deposit, undepositedFundsAccountId);

        var candidates = new List<UndepositedPaymentLineCandidate>();
        var payments = await _accountingRepository.GetPaymentsByOfficeIdsAsync(
            deposit.OrganizationId,
            deposit.OfficeId.ToString(),
            (int)PaymentKind.Invoice);

        foreach (var payment in payments.Where(payment => payment.IsActive))
        {
            var paymentEntries = await GetJournalEntriesByPaymentIdCachedAsync(deposit.OrganizationId, payment.PaymentId);
            foreach (var paymentEntry in paymentEntries)
            {
                if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                    continue;

                AppendUndepositedPaymentLineCandidates(
                    candidates,
                    paymentEntry,
                    undepositedFundsAccountId,
                    payment.DepositId);
            }
        }

        return candidates;
    }

    private static void AppendUndepositedPaymentLineCandidates(
        ICollection<UndepositedPaymentLineCandidate> candidates,
        JournalEntry paymentEntry,
        int undepositedFundsAccountId,
        Guid? paymentDepositId)
    {
        var sourceCode = ResolvePaymentJournalEntrySourceCode(paymentEntry);
        if (string.IsNullOrWhiteSpace(sourceCode))
            return;

        foreach (var line in paymentEntry.JournalEntryLines ?? [])
        {
            if (line.ChartOfAccountId != undepositedFundsAccountId)
                continue;

            var netAmount = line.Debit - line.Credit;
            if (Math.Abs(netAmount) <= 0.005m)
                continue;

            candidates.Add(new UndepositedPaymentLineCandidate
            {
                JournalEntryLineId = line.JournalEntryLineId,
                NetAmount = netAmount,
                PropertyId = NormalizeOptionalGuid(line.PropertyId),
                ReservationId = NormalizeOptionalGuid(line.ReservationId),
                ContactId = NormalizeOptionalGuid(line.ContactId),
                DepositId = NormalizeOptionalGuid(paymentEntry.DepositId ?? paymentDepositId),
                SourceCode = sourceCode,
                TransactionDate = paymentEntry.TransactionDate
            });
        }
    }

    private async Task<HashSet<Guid>> GetJournalEntryLineIdsClaimedByOtherDepositsAsync(Deposit deposit)
    {
        if (_officeSyncCache != null)
            return _officeSyncCache.GetClaimedDepositLineIdsExcluding(deposit.DepositId);

        var claimedLineIds = new HashSet<Guid>();
        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = deposit.OrganizationId,
            OfficeIds = deposit.OfficeId.ToString(),
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        foreach (var otherDeposit in deposits)
        {
            if (otherDeposit.DepositId == deposit.DepositId || otherDeposit.IsActive == false)
                continue;

            foreach (var split in otherDeposit.Splits ?? [])
            {
                if (split.JournalEntryLineId is { } journalEntryLineId && journalEntryLineId != Guid.Empty)
                    claimedLineIds.Add(journalEntryLineId);
            }
        }

        return claimedLineIds;
    }

    private async Task<bool> IsValidDepositSplitJournalEntryLineAsync(Guid organizationId, DepositSplit split, int undepositedFundsAccountId)
    {
        if (split.JournalEntryLineId is not { } journalEntryLineId || journalEntryLineId == Guid.Empty)
            return false;

        var line = await GetJournalEntryLineByIdCachedAsync(journalEntryLineId);
        if (line == null)
            return false;

        var accountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId.Value : undepositedFundsAccountId;
        if (line.ChartOfAccountId != accountId)
            return false;

        if (!DepositSplitMatchesUndepositedLineAmount(split, line.Debit - line.Credit))
            return false;

        // Payment-backed splits: linked UF line must belong to that invoice's payment JE.
        var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        if (string.IsNullOrWhiteSpace(splitSourceCode))
            return true;

        var paymentJournalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, organizationId);
        if (paymentJournalEntry == null)
            return false;

        var paymentSourceCode = ResolvePaymentJournalEntrySourceCode(paymentJournalEntry);
        if (!string.Equals(paymentSourceCode, splitSourceCode, StringComparison.OrdinalIgnoreCase))
            return false;

        // Property on the payment line is often blank; only reject when both sides have different properties.
        var splitPropertyId = NormalizeOptionalGuid(split.PropertyId);
        var linePropertyId = NormalizeOptionalGuid(line.PropertyId);
        if (splitPropertyId != null && linePropertyId != null && splitPropertyId != linePropertyId)
            return false;

        return true;
    }

    private async Task<Guid?> ResolveDepositSplitByDepositedPaymentStampAsync(
        Deposit deposit,
        DepositSplit split,
        int undepositedFundsAccountId,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
            return null;

        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return null;

        var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        var payments = await GetInvoicePaymentsStampedToDepositAsync(deposit);

        Guid? bestLineId = null;
        var bestRank = int.MaxValue;

        foreach (var payment in payments)
        {
            if (payment.DepositId != deposit.DepositId)
                continue;

            var paymentEntries = (await GetJournalEntriesByPaymentIdCachedAsync(deposit.OrganizationId, payment.PaymentId))
                .Where(IsRematchableHealthInvoicePaymentJournalEntry);

            foreach (var paymentEntry in paymentEntries)
            {
                var paymentSourceCode = ResolvePaymentJournalEntrySourceCode(paymentEntry);
                foreach (var line in paymentEntry.JournalEntryLines ?? [])
                {
                    if (line.ChartOfAccountId != undepositedFundsAccountId)
                        continue;

                    var netAmount = Math.Abs(line.Debit - line.Credit);
                    if (netAmount <= 0.005m)
                        continue;

                    if (Math.Abs(netAmount - splitAmount) > 0.005m)
                        continue;

                    if (line.JournalEntryLineId == Guid.Empty)
                        continue;

                    if (claimedLineIds.Contains(line.JournalEntryLineId) || assignedLineIds.Contains(line.JournalEntryLineId))
                        continue;

                    var rank = 0;
                    if (!string.IsNullOrWhiteSpace(splitSourceCode)
                        && string.Equals(paymentSourceCode, splitSourceCode, StringComparison.OrdinalIgnoreCase))
                    {
                        rank -= 2;
                    }

                    if (rank < bestRank)
                    {
                        bestRank = rank;
                        bestLineId = line.JournalEntryLineId;
                    }
                }
            }
        }

        return bestLineId;
    }

    private async Task<List<Payment>> GetInvoicePaymentsStampedToDepositAsync(Deposit deposit)
    {
        if (_officeSyncCache != null)
        {
            return _officeSyncCache.Payments
                .Where(payment =>
                    payment.IsActive
                    && payment.PaymentKindId == (int)PaymentKind.Invoice
                    && payment.DepositId == deposit.DepositId)
                .ToList();
        }

        return (await _accountingRepository.GetPaymentsByOfficeIdsAsync(
                deposit.OrganizationId,
                deposit.OfficeId.ToString(),
                (int)PaymentKind.Invoice))
            .Where(payment => payment.IsActive && payment.DepositId == deposit.DepositId)
            .ToList();
    }

    private static Guid? ResolveDepositSplitJournalEntryLineId(Deposit deposit, DepositSplit split, IReadOnlyList<UndepositedPaymentLineCandidate> candidates, IReadOnlySet<Guid> claimedLineIds, IReadOnlySet<Guid> assignedLineIds)
    {
        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return null;

        var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        if (string.IsNullOrWhiteSpace(splitSourceCode))
            return null;

        var splitPropertyId = NormalizeOptionalGuid(split.PropertyId);

        // Hard key: invoice + amount. Property narrows when the payment line also has it.
        var invoiceAmountMatches = candidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(Math.Abs(candidate.NetAmount) - splitAmount) <= 0.005m
                && string.Equals(candidate.SourceCode, splitSourceCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (invoiceAmountMatches.Count == 0)
        {
            var invoiceOnlyMatches = candidates
                .Where(candidate =>
                    !claimedLineIds.Contains(candidate.JournalEntryLineId)
                    && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                    && string.Equals(candidate.SourceCode, splitSourceCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (invoiceOnlyMatches.Count == 1)
                return invoiceOnlyMatches[0].JournalEntryLineId;

            if (invoiceOnlyMatches.Count > 1)
            {
                var closestAmountMatches = invoiceOnlyMatches
                    .OrderBy(candidate => Math.Abs(Math.Abs(candidate.NetAmount) - splitAmount))
                    .ThenByDescending(candidate => candidate.DepositId == deposit.DepositId ? 1 : 0)
                    .ThenBy(candidate => candidate.JournalEntryLineId)
                    .ToList();

                var best = closestAmountMatches[0];
                var bestDelta = Math.Abs(Math.Abs(best.NetAmount) - splitAmount);
                if (closestAmountMatches.Count == 1
                    || Math.Abs(Math.Abs(closestAmountMatches[1].NetAmount) - splitAmount) - bestDelta > 0.005m)
                {
                    return best.JournalEntryLineId;
                }
            }

            return null;
        }

        if (invoiceAmountMatches.Count == 1)
            return invoiceAmountMatches[0].JournalEntryLineId;

        if (splitPropertyId != null)
        {
            var exactPropertyMatches = invoiceAmountMatches
                .Where(candidate => GuidEquals(splitPropertyId, candidate.PropertyId))
                .ToList();
            if (exactPropertyMatches.Count == 1)
                return exactPropertyMatches[0].JournalEntryLineId;

            if (exactPropertyMatches.Count > 1)
                invoiceAmountMatches = exactPropertyMatches;
        }

        // Same invoice/amount should be rare; prefer deposit-stamped then closest date.
        return invoiceAmountMatches
            .OrderByDescending(candidate => candidate.DepositId == deposit.DepositId ? 1 : 0)
            .ThenBy(candidate => Math.Abs(candidate.TransactionDate.DayNumber - deposit.DepositDate.DayNumber))
            .ThenBy(candidate => candidate.JournalEntryLineId)
            .First()
            .JournalEntryLineId;
    }

    private static string? ResolveDepositSplitInvoiceSourceCode(DepositSplit split)
    {
        var description = (split.Description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var paymentMemoMatch = MatchPaymentMemo(description);
        if (paymentMemoMatch.IsMatch && !string.IsNullOrWhiteSpace(paymentMemoMatch.SourceCode))
            return paymentMemoMatch.SourceCode.Trim();

        var prepaymentMemoMatch = MatchPrePaymentMemo(description);
        if (prepaymentMemoMatch.IsMatch && !string.IsNullOrWhiteSpace(prepaymentMemoMatch.SourceCode))
            return prepaymentMemoMatch.SourceCode.Trim();

        if (TryParseInvoiceSourceCodeFromMemo(description, out var invoiceSourceCode))
            return invoiceSourceCode;

        var colonIndex = description.IndexOf(':');
        if (colonIndex > 0)
            return description[..colonIndex].Trim();

        return null;
    }

    private static string ResolvePaymentJournalEntrySourceCode(JournalEntry paymentEntry)
    {
        if (!string.IsNullOrWhiteSpace(paymentEntry.SourceCode))
            return paymentEntry.SourceCode.Trim();

        var memoMatch = MatchPaymentMemo(paymentEntry.Memo, paymentEntry.JournalEntryLines?.FirstOrDefault()?.Memo);
        if (memoMatch.IsMatch && !string.IsNullOrWhiteSpace(memoMatch.SourceCode))
            return memoMatch.SourceCode.Trim();

        var prepaymentMemoMatch = MatchPrePaymentMemo(paymentEntry.Memo, paymentEntry.JournalEntryLines?.FirstOrDefault()?.Memo);
        if (prepaymentMemoMatch.IsMatch && !string.IsNullOrWhiteSpace(prepaymentMemoMatch.SourceCode))
            return prepaymentMemoMatch.SourceCode.Trim();

        if (TryParseInvoiceSourceCodeFromMemo(CoalesceJournalEntryMemo(paymentEntry.Memo, paymentEntry.JournalEntryLines?.FirstOrDefault()?.Memo), out var invoiceSourceCode))
            return invoiceSourceCode;

        return string.Empty;
    }

    private static bool DepositSplitMatchesUndepositedLineAmount(DepositSplit split, decimal lineNetAmount)
        => Math.Abs(Math.Abs(lineNetAmount) - Math.Abs(RoundCurrency(split.Amount))) <= 0.005m;

    private static bool GuidEquals(Guid? left, Guid? right)
    {
        var normalizedLeft = NormalizeOptionalGuid(left);
        var normalizedRight = NormalizeOptionalGuid(right);
        return normalizedLeft != null && normalizedRight != null && normalizedLeft == normalizedRight;
    }

    private static bool DepositSplitJournalEntryLineIdsChanged(IReadOnlyList<Guid?> originalLineIds, IReadOnlyList<DepositSplit>? reconciledSplits)
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

using System.Text.RegularExpressions;
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
        public DateOnly AccountingPeriod { get; init; }
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

        var paymentLineCandidates = (await BuildUndepositedPaymentLineCandidatesAsync(deposit, undepositedFundsAccountId))
            .Where(candidate =>
                IsPaymentDepositStampAvailableForDeposit(candidate.DepositId, deposit.DepositId)
                && MatchesAccountingPeriodMonth(
                    candidate.AccountingPeriod,
                    candidate.TransactionDate,
                    deposit.AccountingPeriod,
                    deposit.DepositDate))
            .ToList();
        var claimedLineIds = await GetJournalEntryLineIdsClaimedByOtherDepositsAsync(deposit);
        var assignedLineIds = new HashSet<Guid>();
        trail?.Note($"Rematch: UF account={undepositedFundsAccountId} candidates={paymentLineCandidates.Count} claimedByOthers={claimedLineIds.Count}");

        await EnsureDepositSplitReservationContextAsync(deposit, trail);

        foreach (var split in deposit.Splits)
        {
            var splitLabel = ResolveDepositSplitInvoiceSourceCode(split) ?? split.DepositSplitId.ToString();
            if (await IsValidDepositSplitJournalEntryLineAsync(deposit, split, undepositedFundsAccountId))
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

            if (await IsValidDepositSplitJournalEntryLineAsync(deposit, split, undepositedFundsAccountId))
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
        var splitReservationId = await ResolveReservationIdForDepositSplitAsync(deposit, split);

        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return null;

        var paymentIds = new HashSet<Guid>();
        foreach (var payment in await GetInvoicePaymentsStampedToDepositAsync(deposit))
            paymentIds.Add(payment.PaymentId);

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

        if (string.IsNullOrWhiteSpace(splitSourceCode) && splitReservationId == null && paymentIds.Count == 0)
            return null;

        paymentIds = await FilterPaymentIdsAvailableForDepositRematchAsync(deposit, paymentIds);
        if (paymentIds.Count == 0)
            return null;

        var matches = new List<(Guid LineId, int Rank)>();
        foreach (var paymentId in paymentIds)
        {
            foreach (var paymentEntry in await GetJournalEntriesByPaymentIdCachedAsync(deposit.OrganizationId, paymentId))
                CollectUndepositedFundsLineMatches(
                    paymentEntry,
                    deposit,
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
                    deposit,
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
                deposit,
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
        Deposit deposit,
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
            foreach (var paymentEntry in await GetJournalEntriesByPaymentIdCachedAsync(deposit.OrganizationId, paymentId))
            {
                if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                    continue;

                if (!JournalEntryMatchesDepositAccountingMonth(paymentEntry, deposit))
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
        Deposit deposit,
        int undepositedFundsAccountId,
        string splitSourceCode,
        decimal splitAmount,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds,
        ICollection<(Guid LineId, int Rank)> matches)
    {
        if (paymentEntry.JournalEntryKindId is not (JournalEntryKind.Payment or JournalEntryKind.PrePaymentReceive))
            return;

        if (!JournalEntryMatchesDepositAccountingMonth(paymentEntry, deposit))
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
                TransactionDate = paymentEntry.TransactionDate,
                AccountingPeriod = paymentEntry.AccountingPeriod
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

    private async Task<bool> IsValidDepositSplitJournalEntryLineAsync(Deposit deposit, DepositSplit split, int undepositedFundsAccountId)
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

        if (!await DepositSplitLineStillPointsAtPaymentAsync(deposit.OrganizationId, journalEntryLineId))
            return false;

        var paymentId = await ResolvePaymentIdFromDepositSplitLineAsync(split, deposit.OrganizationId);
        if (paymentId == Guid.Empty)
            return false;

        if (await PaymentDepositStampConflictsWithDepositAsync(deposit, paymentId))
            return false;

        var paymentJournalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, deposit.OrganizationId);
        if (paymentJournalEntry == null || !JournalEntryMatchesDepositAccountingMonth(paymentJournalEntry, deposit))
            return false;

        var splitPropertyId = NormalizeOptionalGuid(split.PropertyId);
        var linePropertyId = NormalizeOptionalGuid(line.PropertyId);
        if (splitPropertyId != null && linePropertyId != null && splitPropertyId != linePropertyId)
            return false;

        return true;
    }

    private static bool IsPaymentDepositStampAvailableForDeposit(Guid? paymentDepositId, Guid depositId)
    {
        if (paymentDepositId is not { } stampedDepositId || stampedDepositId == Guid.Empty)
            return true;

        return stampedDepositId == depositId;
    }

    private async Task<bool> PaymentDepositStampConflictsWithDepositAsync(Deposit deposit, Guid paymentId)
    {
        if (paymentId == Guid.Empty)
            return false;

        Payment? payment = null;
        if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
            payment = cachedPayment;
        else
            payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, deposit.OrganizationId);

        if (payment?.DepositId is not { } paymentDepositId || paymentDepositId == Guid.Empty)
            return false;

        return paymentDepositId != deposit.DepositId;
    }

    private async Task<HashSet<Guid>> FilterPaymentIdsAvailableForDepositRematchAsync(
        Deposit deposit,
        IReadOnlyCollection<Guid> paymentIds)
    {
        var eligible = new HashSet<Guid>();
        foreach (var paymentId in paymentIds)
        {
            if (paymentId == Guid.Empty)
                continue;

            if (!await PaymentDepositStampConflictsWithDepositAsync(deposit, paymentId))
                eligible.Add(paymentId);
        }

        return eligible;
    }

    private async Task<bool> DepositSplitLineStillPointsAtPaymentAsync(Guid organizationId, Guid journalEntryLineId)
    {
        var line = await GetJournalEntryLineByIdCachedAsync(journalEntryLineId);
        if (line == null || line.JournalEntryId == Guid.Empty)
            return false;

        var paymentJournalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, organizationId);
        if (paymentJournalEntry?.PaymentId is { } cachedPaymentId && cachedPaymentId != Guid.Empty)
            return true;

        paymentJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(line.JournalEntryId, organizationId);
        return paymentJournalEntry?.PaymentId is { } paymentId && paymentId != Guid.Empty;
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
        var matches = new List<(Guid LineId, int Rank)>();

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

                    matches.Add((line.JournalEntryLineId, rank));
                }
            }
        }

        return ResolveUniqueDepositSplitLineMatch(matches);
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
        var splitPropertyId = NormalizeOptionalGuid(split.PropertyId);

        if (string.IsNullOrWhiteSpace(splitSourceCode))
            return ResolveUniqueDepositStampedAmountLineMatch(deposit, splitAmount, candidates, claimedLineIds, assignedLineIds);

        // Hard key: invoice + amount. Property narrows when the payment line also has it.
        var invoiceAmountMatches = candidates
            .Where(candidate =>
                IsPaymentDepositStampAvailableForDeposit(candidate.DepositId, deposit.DepositId)
                && !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(Math.Abs(candidate.NetAmount) - splitAmount) <= 0.005m
                && string.Equals(candidate.SourceCode, splitSourceCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (invoiceAmountMatches.Count == 0)
        {
            var invoiceOnlyMatches = candidates
                .Where(candidate =>
                    IsPaymentDepositStampAvailableForDeposit(candidate.DepositId, deposit.DepositId)
                    && !claimedLineIds.Contains(candidate.JournalEntryLineId)
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

            return ResolveUniqueDepositStampedAmountLineMatch(deposit, splitAmount, candidates, claimedLineIds, assignedLineIds);
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

        if (invoiceAmountMatches.Count == 1)
            return invoiceAmountMatches[0].JournalEntryLineId;

        if (invoiceAmountMatches.Count > 1)
        {
            var uniqueRankedMatch = ResolveUniqueDepositSplitLineMatch(
                invoiceAmountMatches
                    .Select((candidate, index) => (
                        candidate.JournalEntryLineId,
                        Rank: (candidate.DepositId == deposit.DepositId ? -1 : 0)
                            + index))
                    .ToList());
            if (uniqueRankedMatch != null)
                return uniqueRankedMatch;
        }

        return ResolveUniqueDepositStampedAmountLineMatch(deposit, splitAmount, candidates, claimedLineIds, assignedLineIds);
    }

    private static Guid? ResolveUniqueDepositStampedAmountLineMatch(
        Deposit deposit,
        decimal splitAmount,
        IReadOnlyList<UndepositedPaymentLineCandidate> candidates,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        var stampedAmountMatches = candidates
            .Where(candidate =>
                candidate.DepositId == deposit.DepositId
                && !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(Math.Abs(candidate.NetAmount) - splitAmount) <= 0.005m)
            .ToList();

        if (stampedAmountMatches.Count == 1)
            return stampedAmountMatches[0].JournalEntryLineId;

        if (stampedAmountMatches.Count > 1)
        {
            var closestDateMatch = stampedAmountMatches
                .OrderBy(candidate => Math.Abs(candidate.TransactionDate.DayNumber - deposit.DepositDate.DayNumber))
                .ThenBy(candidate => candidate.JournalEntryLineId)
                .ToList();

            var best = closestDateMatch[0];
            var second = closestDateMatch[1];
            if (Math.Abs(closestDateMatch[0].TransactionDate.DayNumber - deposit.DepositDate.DayNumber)
                < Math.Abs(second.TransactionDate.DayNumber - deposit.DepositDate.DayNumber))
            {
                return best.JournalEntryLineId;
            }
        }

        return null;
    }

    private static Guid? ResolveUniqueDepositSplitLineMatch(IReadOnlyList<(Guid LineId, int Rank)> matches)
    {
        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0].LineId;

        var ordered = matches
            .OrderBy(match => match.Rank)
            .ThenBy(match => match.LineId)
            .ToList();

        if (ordered[0].Rank < ordered[1].Rank)
            return ordered[0].LineId;

        return null;
    }

    private sealed class DepositSplitReservationContext
    {
        public Guid ReservationId { get; init; }
        public Guid? PropertyId { get; init; }
    }

    private async Task EnsureDepositSplitReservationContextAsync(Deposit deposit, AccountingSyncBailTrail? trail)
    {
        foreach (var split in deposit.Splits ?? [])
        {
            var needsReservation = NormalizeOptionalGuid(split.ReservationId) == null;
            var needsProperty = NormalizeOptionalGuid(split.PropertyId) == null;
            if (!needsReservation && !needsProperty)
                continue;

            DepositSplitReservationContext? context = null;
            if (needsReservation)
            {
                context = await ResolveDepositSplitReservationContextAsync(deposit, split);
                if (context == null)
                    continue;

                split.ReservationId = context.ReservationId;
            }

            if (!needsProperty)
                continue;

            var propertyId = context?.PropertyId ?? NormalizeOptionalGuid(split.PropertyId);
            if (propertyId == null)
            {
                var reservationId = NormalizeOptionalGuid(split.ReservationId);
                if (reservationId != null)
                    propertyId = await ResolvePropertyIdForReservationAsync(reservationId.Value, deposit.OrganizationId);
            }

            if (propertyId == null)
                continue;

            split.PropertyId = propertyId;
            trail?.Note(
                $"Set reservation/property on split {split.DepositSplitId} from {ResolveDepositSplitReservationSourceCode(split) ?? "description"}.");
        }
    }

    private async Task<Guid?> ResolveReservationIdForDepositSplitAsync(Deposit deposit, DepositSplit split)
    {
        var reservationId = NormalizeOptionalGuid(split.ReservationId);
        if (reservationId != null)
            return reservationId;

        return (await ResolveDepositSplitReservationContextAsync(deposit, split))?.ReservationId;
    }

    private async Task<DepositSplitReservationContext?> ResolveDepositSplitReservationContextAsync(
        Deposit deposit,
        DepositSplit split)
    {
        var invoiceSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        if (!string.IsNullOrWhiteSpace(invoiceSourceCode))
        {
            var invoiceId = await ResolveInvoiceIdBySourceCodeAsync(
                deposit.OrganizationId,
                deposit.OfficeId,
                invoiceSourceCode);
            if (invoiceId != Guid.Empty)
            {
                if (_officeSyncCache != null
                    && _officeSyncCache.InvoicesById.TryGetValue(invoiceId, out var cachedInvoice)
                    && cachedInvoice.ReservationId is { } cachedReservationId
                    && cachedReservationId != Guid.Empty)
                {
                    return new DepositSplitReservationContext
                    {
                        ReservationId = cachedReservationId,
                        PropertyId = NormalizeOptionalGuid(cachedInvoice.PropertyId)
                            ?? await ResolvePropertyIdForReservationAsync(cachedReservationId, deposit.OrganizationId)
                    };
                }

                var invoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
                {
                    OrganizationId = deposit.OrganizationId,
                    OfficeIds = deposit.OfficeId.ToString(),
                    IncludeInactive = true,
                    IncludePaid = true
                });
                var invoice = invoices.FirstOrDefault(item => item.InvoiceId == invoiceId);
                if (invoice?.ReservationId is { } resolvedFromInvoice && resolvedFromInvoice != Guid.Empty)
                {
                    return new DepositSplitReservationContext
                    {
                        ReservationId = resolvedFromInvoice,
                        PropertyId = NormalizeOptionalGuid(invoice.PropertyId)
                            ?? await ResolvePropertyIdForReservationAsync(resolvedFromInvoice, deposit.OrganizationId)
                    };
                }
            }
        }

        var reservationSourceCode = ResolveDepositSplitReservationSourceCode(split);
        if (string.IsNullOrWhiteSpace(reservationSourceCode))
            return null;

        if (_officeSyncCache != null)
        {
            foreach (var invoice in _officeSyncCache.InvoicesById.Values)
            {
                if (!string.Equals(invoice.ReservationCode, reservationSourceCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (invoice.ReservationId is { } cachedReservationId && cachedReservationId != Guid.Empty)
                {
                    return new DepositSplitReservationContext
                    {
                        ReservationId = cachedReservationId,
                        PropertyId = NormalizeOptionalGuid(invoice.PropertyId)
                            ?? await ResolvePropertyIdForReservationAsync(cachedReservationId, deposit.OrganizationId)
                    };
                }
            }
        }

        var reservations = await _reservationRepository.GetActiveReservationsByOfficeIdsAsync(
            deposit.OrganizationId,
            deposit.OfficeId.ToString());

        var activeMatch = reservations.FirstOrDefault(reservation =>
            string.Equals(reservation.ReservationCode, reservationSourceCode, StringComparison.OrdinalIgnoreCase));
        if (activeMatch?.ReservationId is { } activeReservationId && activeReservationId != Guid.Empty)
        {
            return new DepositSplitReservationContext
            {
                ReservationId = activeReservationId,
                PropertyId = NormalizeOptionalGuid(activeMatch.PropertyId)
            };
        }

        var reservationLists = await _reservationRepository.GetReservationListByOfficeIdAsync(
            deposit.OrganizationId,
            deposit.OfficeId.ToString());

        var listedMatch = reservationLists.FirstOrDefault(reservation =>
            string.Equals(reservation.ReservationCode, reservationSourceCode, StringComparison.OrdinalIgnoreCase));
        if (listedMatch?.ReservationId is { } listedReservationId && listedReservationId != Guid.Empty)
        {
            return new DepositSplitReservationContext
            {
                ReservationId = listedReservationId,
                PropertyId = NormalizeOptionalGuid(listedMatch.PropertyId)
            };
        }

        return null;
    }

    private async Task<Guid?> ResolvePropertyIdForReservationAsync(Guid reservationId, Guid organizationId)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId);
        return NormalizeOptionalGuid(reservation?.PropertyId);
    }

    private static string? ResolveDepositSplitReservationSourceCode(DepositSplit split)
    {
        var invoiceCode = ResolveDepositSplitInvoiceSourceCode(split);
        if (string.IsNullOrWhiteSpace(invoiceCode))
            return null;

        var invoicePatternMatch = InvoiceReservationSourceCodePattern.Match(invoiceCode);
        if (invoicePatternMatch.Success)
            return invoicePatternMatch.Groups[1].Value;

        return invoiceCode;
    }

    private static readonly Regex InvoiceReservationSourceCodePattern =
        new(@"^(R-\d+)-\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    private static bool DepositSplitReservationIdsChanged(
        IReadOnlyList<Guid?> originalReservationIds,
        IReadOnlyList<DepositSplit>? reconciledSplits)
    {
        var currentReservationIds = (reconciledSplits ?? [])
            .Select(split => split.ReservationId)
            .ToList();

        if (originalReservationIds.Count != currentReservationIds.Count)
            return true;

        for (var index = 0; index < originalReservationIds.Count; index++)
        {
            if (NormalizeOptionalGuid(originalReservationIds[index]) != NormalizeOptionalGuid(currentReservationIds[index]))
                return true;
        }

        return false;
    }

    private static bool DepositSplitPropertyIdsChanged(
        IReadOnlyList<Guid?> originalPropertyIds,
        IReadOnlyList<DepositSplit>? reconciledSplits)
    {
        var currentPropertyIds = (reconciledSplits ?? [])
            .Select(split => split.PropertyId)
            .ToList();

        if (originalPropertyIds.Count != currentPropertyIds.Count)
            return true;

        for (var index = 0; index < originalPropertyIds.Count; index++)
        {
            if (NormalizeOptionalGuid(originalPropertyIds[index]) != NormalizeOptionalGuid(currentPropertyIds[index]))
                return true;
        }

        return false;
    }

    private static bool DepositSplitReconciliationChanged(
        IReadOnlyList<Guid?> originalLineIds,
        IReadOnlyList<Guid?> originalReservationIds,
        IReadOnlyList<Guid?> originalPropertyIds,
        IReadOnlyList<DepositSplit>? reconciledSplits)
        => DepositSplitJournalEntryLineIdsChanged(originalLineIds, reconciledSplits)
            || DepositSplitReservationIdsChanged(originalReservationIds, reconciledSplits)
            || DepositSplitPropertyIdsChanged(originalPropertyIds, reconciledSplits);
}

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

    private async Task ReconcileDepositSplitJournalEntryLineIdsAsync(Deposit deposit)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0 || deposit.OfficeId <= 0)
            return;

        if (!await IsAccountingFeatureEnabledAsync(deposit.OrganizationId))
            return;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            return;

        var paymentLineCandidates = await BuildUndepositedPaymentLineCandidatesAsync(deposit, undepositedFundsAccountId);
        var claimedLineIds = await GetJournalEntryLineIdsClaimedByOtherDepositsAsync(deposit);
        var assignedLineIds = new HashSet<Guid>();

        foreach (var split in deposit.Splits)
        {
            if (await IsValidDepositSplitJournalEntryLineAsync(deposit.OrganizationId, split, undepositedFundsAccountId))
            {
                if (split.JournalEntryLineId.HasValue && split.JournalEntryLineId != Guid.Empty)
                    assignedLineIds.Add(split.JournalEntryLineId.Value);

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
                continue;
            }

            // Stale after clear/resync (or wrong account line): clear so callers rematch instead of treating as valid.
            if (split.JournalEntryLineId is { } staleLineId && staleLineId != Guid.Empty)
                split.JournalEntryLineId = null;
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

    private async Task<List<UndepositedPaymentLineCandidate>> BuildUndepositedPaymentLineCandidatesAsync(Deposit deposit, int undepositedFundsAccountId)
    {
        var paymentEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = deposit.OrganizationId,
            OfficeIds = deposit.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Invoice,
            IncludeUnposted = true
        }))
            .Where(entry =>
                IsStandardInvoicePaymentJournalEntry(entry)
                || entry.JournalEntryKindId == JournalEntryKind.PrePaymentReceive)
            .ToList();

        var candidates = new List<UndepositedPaymentLineCandidate>();
        foreach (var paymentEntry in paymentEntries)
        {
            var sourceCode = ResolvePaymentJournalEntrySourceCode(paymentEntry);
            if (string.IsNullOrWhiteSpace(sourceCode))
                continue;

            foreach (var line in paymentEntry.JournalEntryLines)
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
                    DepositId = NormalizeOptionalGuid(paymentEntry.DepositId),
                    SourceCode = sourceCode,
                    TransactionDate = paymentEntry.TransactionDate
                });
            }
        }

        return candidates;
    }

    private async Task<HashSet<Guid>> GetJournalEntryLineIdsClaimedByOtherDepositsAsync(Deposit deposit)
    {
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

        var line = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
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

        var paymentJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(line.JournalEntryId, organizationId);
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
            return null;

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

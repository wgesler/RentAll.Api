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
            if (await IsValidDepositSplitJournalEntryLineAsync(split, undepositedFundsAccountId))
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

            if (await IsValidDepositSplitJournalEntryLineAsync(split, undepositedFundsAccountId))
                continue;

            messages.Add(
                $"Deposit {depositLabel}: payment split amount {split.Amount:0.00} is not linked to a valid undeposited payment line.");
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
            .Where(entry => IsStandardInvoicePaymentJournalEntry(entry))
            .ToList();

        var candidates = new List<UndepositedPaymentLineCandidate>();
        foreach (var paymentEntry in paymentEntries)
        {
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

    private async Task<bool> IsValidDepositSplitJournalEntryLineAsync(DepositSplit split, int undepositedFundsAccountId)
    {
        if (split.JournalEntryLineId is not { } journalEntryLineId || journalEntryLineId == Guid.Empty)
            return false;

        var line = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
        if (line == null)
            return false;

        var accountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId.Value : undepositedFundsAccountId;
        if (line.ChartOfAccountId != accountId)
            return false;

        return DepositSplitMatchesUndepositedLineAmount(split, line.Debit - line.Credit);
    }

    private static Guid? ResolveDepositSplitJournalEntryLineId(Deposit deposit, DepositSplit split, IReadOnlyList<UndepositedPaymentLineCandidate> candidates, IReadOnlySet<Guid> claimedLineIds, IReadOnlySet<Guid> assignedLineIds)
    {
        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return null;

        // Amount match among unclaimed lines — always pick the best candidate (never leave ties unresolved).
        var amountMatches = candidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(Math.Abs(candidate.NetAmount) - splitAmount) <= 0.005m)
            .ToList();

        if (amountMatches.Count == 0)
            return null;

        return amountMatches
            .OrderByDescending(candidate => candidate.DepositId == deposit.DepositId ? 1 : 0)
            .ThenByDescending(candidate => ScoreDepositSplitContext(split, candidate.PropertyId, candidate.ReservationId, candidate.ContactId))
            .ThenBy(candidate => Math.Abs(candidate.TransactionDate.DayNumber - deposit.DepositDate.DayNumber))
            .ThenBy(candidate => candidate.JournalEntryLineId)
            .First()
            .JournalEntryLineId;
    }

    private static bool DepositSplitMatchesUndepositedLineAmount(DepositSplit split, decimal lineNetAmount)
        => Math.Abs(Math.Abs(lineNetAmount) - Math.Abs(RoundCurrency(split.Amount))) <= 0.005m;

    private static int ScoreDepositSplitContext(DepositSplit split, Guid? propertyId, Guid? reservationId, Guid? contactId)
    {
        var score = 0;
        if (GuidEquals(split.PropertyId, propertyId))
            score += 4;
        if (GuidEquals(split.ReservationId, reservationId))
            score += 2;
        if (GuidEquals(split.ContactId, contactId))
            score += 1;
        return score;
    }

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

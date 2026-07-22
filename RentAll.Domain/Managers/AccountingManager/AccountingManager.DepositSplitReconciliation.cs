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
        if (paymentLineCandidates.Count == 0)
            return;

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

            var resolvedLineId = ResolveDepositSplitJournalEntryLineId(
                deposit,
                split,
                paymentLineCandidates,
                claimedLineIds,
                assignedLineIds);

            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                split.JournalEntryLineId = resolvedLineId;
                assignedLineIds.Add(resolvedLineId.Value);
            }
        }
    }

    private async Task<List<UndepositedPaymentLineCandidate>> BuildUndepositedPaymentLineCandidatesAsync(
        Deposit deposit,
        int undepositedFundsAccountId)
    {
        var paymentEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = deposit.OrganizationId,
            OfficeIds = deposit.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Invoice,
            IncludeVoided = false,
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

        return DepositSplitMatchesUndepositedLine(split, line);
    }

    private static Guid? ResolveDepositSplitJournalEntryLineId(
        Deposit deposit,
        DepositSplit split,
        IReadOnlyList<UndepositedPaymentLineCandidate> candidates,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        var matches = candidates
            .Where(candidate =>
                !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && DepositSplitMatchesUndepositedCandidate(split, candidate))
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0].JournalEntryLineId;

        var depositDate = deposit.DepositDate;
        var closestMatches = matches
            .OrderBy(candidate => Math.Abs((candidate.TransactionDate.DayNumber - depositDate.DayNumber)))
            .ThenBy(candidate => candidate.JournalEntryLineId)
            .ToList();

        var bestDistance = Math.Abs(closestMatches[0].TransactionDate.DayNumber - depositDate.DayNumber);
        var tiedMatches = closestMatches
            .Where(candidate => Math.Abs(candidate.TransactionDate.DayNumber - depositDate.DayNumber) == bestDistance)
            .ToList();

        return tiedMatches.Count == 1 ? tiedMatches[0].JournalEntryLineId : null;
    }

    private static bool DepositSplitMatchesUndepositedLine(DepositSplit split, JournalEntryLine line)
        => Math.Abs((line.Debit - line.Credit) - split.Amount) <= 0.005m
            && DepositSplitContextMatches(split, line.PropertyId, line.ReservationId, line.ContactId);

    private static bool DepositSplitMatchesUndepositedCandidate(DepositSplit split, UndepositedPaymentLineCandidate candidate)
        => Math.Abs(candidate.NetAmount - split.Amount) <= 0.005m
            && DepositSplitContextMatches(split, candidate.PropertyId, candidate.ReservationId, candidate.ContactId);

    private static bool DepositSplitContextMatches(
        DepositSplit split,
        Guid? propertyId,
        Guid? reservationId,
        Guid? contactId)
    {
        if (!GuidMatches(split.PropertyId, propertyId))
            return false;

        if (!GuidMatches(split.ReservationId, reservationId))
            return false;

        return GuidMatches(split.ContactId, contactId);
    }

    private static bool GuidMatches(Guid? expected, Guid? actual)
    {
        var normalizedExpected = NormalizeOptionalGuid(expected);
        var normalizedActual = NormalizeOptionalGuid(actual);
        if (normalizedExpected == null || normalizedActual == null)
            return true;

        return normalizedExpected == normalizedActual;
    }

    private static bool DepositSplitJournalEntryLineIdsChanged(
        IReadOnlyList<Guid?> originalLineIds,
        IReadOnlyList<DepositSplit>? reconciledSplits)
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

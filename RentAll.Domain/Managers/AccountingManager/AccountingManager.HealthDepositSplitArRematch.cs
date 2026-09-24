using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task ApplyDepositSplitArRematchCandidatesForHealthFixAsync(
        Deposit deposit,
        Guid organizationId,
        Guid currentUser,
        JournalEntrySyncResult result,
        AccountingSyncBailTrail? trail)
    {
        var candidates = await _accountingRepository.GetDepositSplitArRematchCandidatesAsync(
            organizationId,
            deposit.DepositId);

        LogHealthDepositFixTrace(
            deposit,
            "ArRematchProc",
            $"Exact={candidates.ExactMatches.Count} NoMatch={candidates.NoMatches.Count}");

        var depositChanged = false;
        var stampedPaymentIds = new HashSet<Guid>();

        foreach (var match in candidates.ExactMatches)
        {
            var split = deposit.Splits?.FirstOrDefault(row => row.DepositSplitId == match.DepositSplitId);
            if (split == null)
                continue;

            if (match.PaymentId != Guid.Empty)
            {
                var needsStamp = match.PaymentDepositId is null
                    || match.PaymentDepositId == Guid.Empty
                    || match.PaymentDepositId != deposit.DepositId;

                if (needsStamp && stampedPaymentIds.Add(match.PaymentId))
                {
                    await _accountingRepository.SetPaymentDepositIdAsync(
                        match.PaymentId,
                        organizationId,
                        deposit.DepositId,
                        currentUser);
                    trail?.Note(
                        $"ArRematch stamp payment {match.PaymentCode ?? match.PaymentId.ToString()} -> deposit {deposit.DepositCode}.");
                }
            }

            if (match.IsConsolidatedPaymentTotal)
                continue;

            if (match.ArJournalEntryLineId is not { } arLineId || arLineId == Guid.Empty)
                continue;

            if (split.JournalEntryLineId is { } existingLineId && existingLineId != Guid.Empty)
                continue;

            split.JournalEntryLineId = arLineId;
            depositChanged = true;
            trail?.Note($"ArRematch link split {split.DepositSplitId} -> line {arLineId}.");
        }

        if (depositChanged)
        {
            deposit.ModifiedBy = currentUser;
            var updated = await _accountingRepository.UpdateDepositAsync(deposit);
            deposit.Splits = updated.Splits;
            _officeSyncCache?.ReplaceDeposit(deposit);
        }

        foreach (var failure in candidates.NoMatches)
        {
            var label = FormatDepositLabel(deposit);
            var detail = string.IsNullOrWhiteSpace(failure.SplitDescription)
                ? failure.MatchOutcome
                : $"{failure.MatchOutcome} — {failure.SplitDescription.Trim()}";
            result.Errors.Add($"Deposit {label}: UF split {failure.DepositSplitId} — {detail} (manual review).");
            trail?.Bail($"ArRematch no match: split {failure.DepositSplitId} — {failure.MatchOutcome}.");
        }

        if (_officeSyncCache != null && stampedPaymentIds.Count > 0)
        {
            foreach (var paymentId in stampedPaymentIds)
            {
                if (_officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                {
                    cachedPayment.DepositId = deposit.DepositId;
                    cachedPayment.DepositCode = deposit.DepositCode;
                }
            }

            _officeSyncCache.InvalidateRematchIndexes();
        }
    }
}

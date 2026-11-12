using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Save-path entry to the same exact AR rematch the deposit health fix applies.
    /// Finds the deposit whose unlinked split is an exact match for this payment
    /// (invoice code, amount, payment date on or before the deposit date, reservation)
    /// and attaches it, so resaving a payment repairs the link without a health fix.
    /// </summary>
    private async Task TryAttachPaymentToExactDepositSplitMatchAsync(Payment payment, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty
            || !payment.IsActive
            || payment.PaymentKindId != (int)Enums.PaymentKind.Invoice)
            return;

        var candidateDeposits = new List<Deposit>();
        if (payment.DepositId is { } stampedDepositId && stampedDepositId != Guid.Empty)
        {
            var stampedDeposit = await _accountingRepository.GetDepositByIdAsync(stampedDepositId, payment.OrganizationId);
            if (stampedDeposit != null)
                candidateDeposits.Add(stampedDeposit);
        }
        else if (_officeSyncCache != null)
        {
            candidateDeposits.AddRange(_officeSyncCache.Deposits
                .Where(deposit => deposit.OfficeId == payment.OfficeId));
        }
        else
        {
            var officeDeposits = await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
            {
                OrganizationId = payment.OrganizationId,
                OfficeIds = payment.OfficeId.ToString(),
                IsActive = true,
                IncludeInactive = false
            });
            if (officeDeposits != null)
                candidateDeposits.AddRange(officeDeposits);
        }

        foreach (var deposit in candidateDeposits
                     .Where(deposit => deposit.IsActive
                                       && deposit.DepositDate >= payment.PaymentDate
                                       && deposit.Splits != null
                                       && deposit.Splits.Any(split => split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty))
                     .OrderBy(deposit => deposit.DepositDate)
                     .ThenBy(deposit => deposit.DepositCode, StringComparer.OrdinalIgnoreCase))
        {
            var candidates = await _accountingRepository.GetDepositSplitArRematchCandidatesAsync(
                payment.OrganizationId,
                deposit.DepositId);
            if (candidates?.ExactMatches == null
                || !candidates.ExactMatches.Any(match => match.PaymentId == payment.PaymentId))
                continue;

            await ApplyDepositSplitArRematchCandidatesForHealthFixAsync(
                deposit,
                payment.OrganizationId,
                currentUser,
                new JournalEntrySyncResult(),
                trail: null);

            var attached = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, payment.OrganizationId);
            if (attached != null)
            {
                payment.DepositId = attached.DepositId;
                payment.DepositCode = attached.DepositCode;
            }

            return;
        }
    }

    /// <summary>
    /// Same apply as Fix_DepositSplit_ArRematchAndStamp.sql @Commit = 1 (ExactApply only).
    /// </summary>
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

        if (candidates.ExactMatches.Count == 0 && candidates.NoMatches.Count == 0)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var stampedPaymentIds = new HashSet<Guid>();
        var paymentIdsToStamp = candidates.StampPaymentIds.Count > 0
            ? candidates.StampPaymentIds
            : candidates.ExactMatches.Select(match => match.PaymentId).Where(id => id != Guid.Empty);

        foreach (var paymentId in paymentIdsToStamp)
        {
            if (paymentId == Guid.Empty || !stampedPaymentIds.Add(paymentId))
                continue;

            await _accountingRepository.SetPaymentDepositIdAsync(
                paymentId,
                organizationId,
                deposit.DepositId,
                currentUser);
            trail?.Note($"ArRematch restamp payment {paymentId} -> deposit {deposit.DepositCode}.");
        }

        foreach (var paymentId in stampedPaymentIds)
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment == null || !payment.IsActive)
                continue;

            payment.DepositId = deposit.DepositId;
            payment.DepositCode = deposit.DepositCode;

            var paymentEntries = await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(
                new JournalEntryGetByPaymentIdCriteria
                {
                    OrganizationId = organizationId,
                    PaymentId = paymentId
                });

            foreach (var journalEntry in paymentEntries)
            {
                await TryUpdateJournalEntryDepositDocumentLinkAsync(journalEntry, deposit, currentUser);
                await TryUpdateJournalEntryPaymentDocumentLinkAsync(journalEntry, payment, currentUser);
            }
        }

        var depositChanged = false;
        var claimedLineIds = new HashSet<Guid>();
        foreach (var match in candidates.ExactMatches.OrderBy(row => row.DepositSplitId))
        {
            if (match.ArJournalEntryLineId is not { } arLineId || arLineId == Guid.Empty)
                continue;

            if (!claimedLineIds.Add(arLineId))
            {
                trail?.Note($"ArRematch skip split {match.DepositSplitId}: line {arLineId} already claimed on this deposit apply.");
                continue;
            }

            var split = deposit.Splits?.FirstOrDefault(row => row.DepositSplitId == match.DepositSplitId);
            if (split == null)
                continue;

            if (split.JournalEntryLineId == arLineId)
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

        if (candidates.ExactMatches.Count > 0)
            result.JournalEntriesCreated += candidates.ExactMatches.Count;
    }
}

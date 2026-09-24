using Microsoft.Extensions.Logging;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private const string HealthDepositFixTracePrefix = "[DepositHealthFixTrace]";
    private const int HealthDepositFixMaxPasses = 2;

    /// <summary>
    /// Health-fix UF split repair: unlink invalid rows, recover scrambled payment stamps, reconcile splits.
    /// </summary>
    private async Task RepairDepositUfSplitLinksForHealthFixAsync(
        Deposit deposit,
        Guid organizationId,
        Guid currentUser,
        JournalEntrySyncResult result,
        AccountingSyncBailTrail trail)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0 || deposit.OfficeId <= 0)
            return;

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
        {
            trail?.Bail("Repair exit: accounting feature disabled.");
            return;
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
        {
            result.Errors.Add($"Deposit {FormatDepositLabel(deposit)}: Undeposited Funds account is not configured for office {deposit.OfficeId}.");
            return;
        }

        for (var pass = 1; pass <= HealthDepositFixMaxPasses; pass++)
        {
            var needsRepairBefore = await CountDepositSplitsNeedingHealthFixLinkRepairAsync(deposit, undepositedFundsAccountId);
            LogHealthDepositFixTrace(deposit, $"Pass{pass}Start", $"NeedsRepairBefore={needsRepairBefore}");

            await UnlinkAmountMismatchedDepositSplitLinksForDepositHealthFixAsync(deposit, currentUser, trail);
            deposit = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, organizationId) ?? deposit;

            var originalSplitLineIds = deposit.Splits.Select(split => split.JournalEntryLineId).ToList();
            var originalSplitReservationIds = deposit.Splits.Select(split => split.ReservationId).ToList();
            var originalSplitPropertyIds = deposit.Splits.Select(split => split.PropertyId).ToList();

            foreach (var split in deposit.Splits)
            {
                if (IsDepositSplitExcludedFromArRematchScopeByMemo(split))
                    continue;

                if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                    continue;

                if (await IsValidDepositSplitJournalEntryLineAsync(deposit, split, undepositedFundsAccountId))
                    continue;

                if (split.JournalEntryLineId is not { } staleLineId || staleLineId == Guid.Empty)
                    continue;

                var staleLine = await GetJournalEntryLineByIdCachedAsync(staleLineId);
                if (staleLine != null && staleLine.ChartOfAccountId == undepositedFundsAccountId)
                {
                    trail?.Note($"HealthFix clear UF split {split.DepositSplitId} line={staleLineId}.");
                    split.JournalEntryLineId = null;
                    continue;
                }

                if (await IsJournalEntryLineOnClosedJournalEntryAsync(organizationId, staleLineId))
                {
                    await RepairPostedDepositSplitStampForHealthFixAsync(
                        deposit,
                        split,
                        organizationId,
                        currentUser,
                        trail);
                    continue;
                }

                trail?.Note($"HealthFix clear invalid split {split.DepositSplitId} line={staleLineId}.");
                split.JournalEntryLineId = null;
            }

            if (DepositSplitReconciliationChanged(
                    originalSplitLineIds,
                    originalSplitReservationIds,
                    originalSplitPropertyIds,
                    deposit.Splits))
            {
                deposit.ModifiedBy = currentUser;
                var cleared = await _accountingRepository.UpdateDepositAsync(deposit);
                deposit.Splits = cleared.Splits;
                _officeSyncCache?.ReplaceDeposit(deposit);
            }

            await ApplyDepositSplitArRematchCandidatesForHealthFixAsync(
                deposit,
                organizationId,
                currentUser,
                result,
                trail);

            await SyncDepositDocumentLinksAsync(deposit, currentUser, unstampMissingPayments: false);
            deposit = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, organizationId) ?? deposit;

            var needsRepairAfter = await CountDepositSplitsNeedingHealthFixLinkRepairAsync(deposit, undepositedFundsAccountId);
            LogHealthDepositFixTrace(deposit, $"Pass{pass}Done", $"NeedsRepairAfter={needsRepairAfter}");

            if (needsRepairAfter == 0)
                return;
        }

        var remaining = await CountDepositSplitsNeedingHealthFixLinkRepairAsync(deposit, undepositedFundsAccountId);
        if (remaining > 0)
        {
            result.Errors.Add(
                $"Deposit {FormatDepositLabel(deposit)}: {remaining} UF split(s) still missing or stale payment line link after fix. {trail?.FormatBailTrail()}");
        }
    }

    private async Task RepairPostedDepositSplitStampForHealthFixAsync(
        Deposit deposit,
        DepositSplit split,
        Guid organizationId,
        Guid currentUser,
        AccountingSyncBailTrail? trail)
    {
        if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
            return;

        var paymentId = await ResolvePaymentIdFromDepositSplitLineAsync(split, organizationId);
        if (paymentId == Guid.Empty)
            return;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || !payment.IsActive)
            return;

        if (payment.DepositId is { } stampedDepositId
            && stampedDepositId != Guid.Empty
            && stampedDepositId != deposit.DepositId)
        {
            return;
        }

        if (payment.DepositId != deposit.DepositId)
        {
            await _accountingRepository.SetPaymentDepositIdAsync(
                paymentId,
                organizationId,
                deposit.DepositId,
                currentUser);
            payment.DepositId = deposit.DepositId;
            payment.DepositCode = deposit.DepositCode;
        }

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

        trail?.Note($"HealthFix repaired posted split {split.DepositSplitId} stamps for payment {payment.PaymentCode}.");
    }

    private async Task TryUpdateJournalEntryPaymentDocumentLinkAsync(
        JournalEntry journalEntry,
        Payment payment,
        Guid currentUser)
    {
        ApplyPaymentDocumentLink(journalEntry, payment);
        journalEntry.ModifiedBy = currentUser;
        await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(journalEntry, requireActiveLines: true);
    }

    private async Task<int> CountDepositSplitsNeedingHealthFixLinkRepairAsync(
        Deposit deposit,
        int undepositedFundsAccountId)
    {
        var count = 0;
        foreach (var split in deposit.Splits ?? [])
        {
            if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                continue;

            if (await IsValidDepositSplitJournalEntryLineAsync(deposit, split, undepositedFundsAccountId))
                continue;

            count++;
        }

        return count;
    }

    private async Task ClearDepositPaymentDateViolationsForOfficeHealthFixAsync(
        Guid organizationId,
        string officeIds,
        Guid currentUser)
    {
        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        }))
            .OrderBy(deposit => deposit.DepositDate)
            .ThenBy(deposit => deposit.DepositCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var deposit in deposits)
        {
            if (deposit.Splits == null || deposit.Splits.Count == 0 || deposit.OfficeId <= 0)
                continue;

            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, deposit.OfficeId);
            var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
            if (undepositedFundsAccountId <= 0)
                continue;

            var originalSplitLineIds = deposit.Splits.Select(split => split.JournalEntryLineId).ToList();
            var originalSplitReservationIds = deposit.Splits.Select(split => split.ReservationId).ToList();
            var originalSplitPropertyIds = deposit.Splits.Select(split => split.PropertyId).ToList();
            var depositChanged = false;

            foreach (var split in deposit.Splits)
            {
                if (IsDepositSplitExcludedFromArRematchScopeByMemo(split))
                    continue;

                if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                    continue;

                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                    continue;

                var paymentId = await ResolvePaymentIdFromDepositSplitLineAsync(split, organizationId);
                if (paymentId == Guid.Empty)
                    continue;

                Payment? payment = null;
                if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                    payment = cachedPayment;
                else
                    payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);

                if (payment == null || PaymentMatchesDepositTransactionDate(deposit, payment.PaymentDate))
                    continue;

                split.JournalEntryLineId = null;
                depositChanged = true;
            }

            if (depositChanged && DepositSplitReconciliationChanged(
                    originalSplitLineIds,
                    originalSplitReservationIds,
                    originalSplitPropertyIds,
                    deposit.Splits))
            {
                deposit.ModifiedBy = currentUser;
                var updated = await _accountingRepository.UpdateDepositAsync(deposit);
                deposit.Splits = updated.Splits;
                _officeSyncCache?.ReplaceDeposit(deposit);
            }

            var stampedPayments = _officeSyncCache != null
                ? _officeSyncCache.Payments.Where(payment =>
                    payment.IsActive
                    && payment.PaymentKindId == (int)PaymentKind.Invoice
                    && payment.DepositId == deposit.DepositId).ToList()
                : (await _accountingRepository.GetPaymentsByOfficeIdsAsync(
                    organizationId,
                    deposit.OfficeId.ToString(),
                    (int)PaymentKind.Invoice))
                    .Where(payment => payment.IsActive && payment.DepositId == deposit.DepositId)
                    .ToList();

            foreach (var payment in stampedPayments)
            {
                if (PaymentMatchesDepositTransactionDate(deposit, payment.PaymentDate))
                    continue;

                await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
            }
        }

        _officeSyncCache?.InvalidateRematchIndexes();
    }

    private async Task ReleaseMisstampedInvoicePaymentsForOfficeHealthFixAsync(
        Guid organizationId,
        string officeIds,
        Guid currentUser)
    {
        var officeIdList = officeIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : (int?)null)
            .Where(officeId => officeId.HasValue)
            .Select(officeId => officeId!.Value)
            .ToHashSet();

        var payments = _officeSyncCache != null
            ? _officeSyncCache.Payments
                .Where(payment =>
                    payment.IsActive
                    && payment.PaymentKindId == (int)PaymentKind.Invoice
                    && officeIdList.Contains(payment.OfficeId)
                    && payment.DepositId is { } depositId
                    && depositId != Guid.Empty)
                .ToList()
            : (await _accountingRepository.GetPaymentsByOfficeIdsAsync(
                organizationId,
                officeIds,
                (int)PaymentKind.Invoice))
                .Where(payment =>
                    payment.IsActive
                    && payment.DepositId is { } depositId
                    && depositId != Guid.Empty)
                .ToList();

        foreach (var payment in payments)
        {
            if (payment.DepositId is not { } depositId || depositId == Guid.Empty)
                continue;

            var deposit = _officeSyncCache?.Deposits.FirstOrDefault(candidate => candidate.DepositId == depositId)
                ?? await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
            if (deposit == null)
            {
                await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
                continue;
            }

            if (!PaymentMatchesDepositTransactionDate(deposit, payment.PaymentDate))
            {
                await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
                continue;
            }

            if (deposit.Splits == null || deposit.Splits.Count == 0)
            {
                await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
                continue;
            }

            var paymentSourceCode = string.Empty;
            foreach (var paymentEntry in await GetJournalEntriesByPaymentIdCachedAsync(organizationId, payment.PaymentId))
            {
                if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                    continue;

                paymentSourceCode = ResolvePaymentJournalEntrySourceCode(paymentEntry);
                if (!string.IsNullOrWhiteSpace(paymentSourceCode))
                    break;
            }

            if (string.IsNullOrWhiteSpace(paymentSourceCode))
                continue;

            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, deposit.OfficeId);
            var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
            if (undepositedFundsAccountId <= 0)
                continue;

            var hasMatchingSplit = false;
            foreach (var split in deposit.Splits)
            {
                if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                    continue;

                var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
                if (string.IsNullOrWhiteSpace(splitSourceCode))
                    continue;

                if (EntityCodeFormatting.CodesMatch(splitSourceCode, paymentSourceCode))
                {
                    hasMatchingSplit = true;
                    break;
                }
            }

            if (!hasMatchingSplit)
                await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
        }

        _officeSyncCache?.InvalidateRematchIndexes();
    }

    private async Task ClearInvalidOfficeDepositSplitLinksForHealthFixAsync(
        Guid organizationId,
        string officeIds,
        Guid currentUser)
    {
        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        }))
            .OrderBy(deposit => deposit.DepositDate)
            .ThenBy(deposit => deposit.DepositCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var deposit in deposits)
        {
            if (deposit.Splits == null || deposit.Splits.Count == 0 || deposit.OfficeId <= 0)
                continue;

            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, deposit.OfficeId);
            var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
            if (undepositedFundsAccountId <= 0)
                continue;

            var originalSplitLineIds = deposit.Splits.Select(split => split.JournalEntryLineId).ToList();
            var originalSplitReservationIds = deposit.Splits.Select(split => split.ReservationId).ToList();
            var originalSplitPropertyIds = deposit.Splits.Select(split => split.PropertyId).ToList();
            var changed = false;

            foreach (var split in deposit.Splits)
            {
                if (IsDepositSplitExcludedFromArRematchScopeByMemo(split))
                    continue;

                if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                    continue;

                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                    continue;

                var linkedLine = await GetJournalEntryLineByIdCachedAsync(lineId);
                if (linkedLine != null && linkedLine.ChartOfAccountId == undepositedFundsAccountId)
                {
                    split.JournalEntryLineId = null;
                    changed = true;
                    continue;
                }

                if (await IsValidDepositSplitJournalEntryLineAsync(deposit, split, undepositedFundsAccountId))
                    continue;

                if (await IsJournalEntryLineOnClosedJournalEntryAsync(organizationId, lineId))
                    continue;

                split.JournalEntryLineId = null;
                changed = true;
            }

            if (!changed || !DepositSplitReconciliationChanged(
                    originalSplitLineIds,
                    originalSplitReservationIds,
                    originalSplitPropertyIds,
                    deposit.Splits))
                continue;

            deposit.ModifiedBy = currentUser;
            var updated = await _accountingRepository.UpdateDepositAsync(deposit);
            deposit.Splits = updated.Splits;
            _officeSyncCache?.ReplaceDeposit(deposit);
        }
    }

    private async Task<List<Guid>> OrderDepositIdsForHealthFixAsync(Guid organizationId, IReadOnlyList<Guid> depositIds)
    {
        if (depositIds.Count <= 1)
            return depositIds.ToList();

        var ordered = new List<(DateOnly DepositDate, string DepositCode, Guid DepositId)>();
        foreach (var depositId in depositIds.Distinct())
        {
            var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
            if (deposit == null)
                continue;

            ordered.Add((
                deposit.DepositDate,
                deposit.DepositCode ?? string.Empty,
                deposit.DepositId));
        }

        return ordered
            .OrderBy(item => item.DepositDate)
            .ThenBy(item => item.DepositCode, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.DepositId)
            .ToList();
    }

    private static string FormatDepositLabel(Deposit deposit)
        => string.IsNullOrWhiteSpace(deposit.DepositCode) ? deposit.DepositId.ToString() : deposit.DepositCode.Trim();

    /// <summary>
    /// Per-split memo exclusions (same patterns as Fix_DepositSplit_ArRematchAndStamp.sql).
    /// </summary>
    private static bool IsDepositSplitExcludedFromArRematchScopeByMemo(DepositSplit split)
    {
        var description = (split.Description ?? string.Empty).Trim();
        if (description.Length == 0)
            return false;

        var lower = description.ToLowerInvariant();
        return lower.StartsWith("intentional books", StringComparison.Ordinal)
            || lower.StartsWith("opening balance", StringComparison.Ordinal)
            || lower.StartsWith("real estate closing", StringComparison.Ordinal)
            || lower.Contains("paymentech", StringComparison.Ordinal)
            || lower.Contains("reimbursement", StringComparison.Ordinal)
            || lower.Contains("rebate", StringComparison.Ordinal)
            || lower.Contains("stl accounting", StringComparison.Ordinal)
            || lower.Contains("repayment", StringComparison.Ordinal);
    }

    private void LogHealthDepositFixTrace(Deposit deposit, string step, string? detail = null)
    {
        _logger.LogError(
            "{Prefix} Step={Step} DepositCode={DepositCode} DepositId={DepositId}{Detail}",
            HealthDepositFixTracePrefix,
            step,
            FormatDepositLabel(deposit),
            deposit.DepositId,
            string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}");
    }
}

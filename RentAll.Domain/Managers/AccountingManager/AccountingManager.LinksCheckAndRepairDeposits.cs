using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Prepare Deposit And Transfer For Save
    private async Task<Deposit> PrepareDepositForSaveAsync(Deposit deposit)
    {
        await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit);
        await EnrichDepositSplitsFromJournalEntryLinesAsync(deposit);
        ApplyDepositHeaderContextFromSplits(deposit);
        return deposit;
    }

    private async Task<Transfer> PrepareTransferForSaveAsync(Transfer transfer)
    {
        await ReconcileTransferSplitJournalEntryLineIdsAsync(transfer);
        await EnrichTransferSplitsFromJournalEntryLinesAsync(transfer);
        ApplyTransferHeaderContextFromSplits(transfer);
        return transfer;
    }
    #endregion

    #region Copy Property Reservation And Contact From The Linked Journal Line
    private async Task EnrichDepositSplitsFromJournalEntryLinesAsync(Deposit deposit)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0)
            return;

        var sourceLineIds = deposit.Splits
            .Where(split => split.JournalEntryLineId.HasValue && split.JournalEntryLineId != Guid.Empty)
            .Select(split => split.JournalEntryLineId!.Value)
            .Distinct()
            .ToList();

        if (sourceLineIds.Count == 0)
            return;

        var sourceLines = await LoadJournalEntryLinesByIdsAsync(sourceLineIds);

        foreach (var split in deposit.Splits)
        {
            if (!split.JournalEntryLineId.HasValue || split.JournalEntryLineId == Guid.Empty)
                continue;

            if (!sourceLines.TryGetValue(split.JournalEntryLineId.Value, out var sourceLine))
                continue;

            ApplyJournalEntryLineContextToDepositSplit(split, sourceLine);
        }
    }

    private async Task EnrichTransferSplitsFromJournalEntryLinesAsync(Transfer transfer)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0)
            return;

        var sourceLineIds = transfer.Splits
            .Where(split => split.JournalEntryLineId.HasValue && split.JournalEntryLineId != Guid.Empty)
            .Select(split => split.JournalEntryLineId!.Value)
            .Distinct()
            .ToList();

        if (sourceLineIds.Count == 0)
            return;

        var sourceLines = await LoadJournalEntryLinesByIdsAsync(sourceLineIds);
        var escrowDepositAccountId = transfer.BankAccountId is > 0 ? transfer.BankAccountId.Value : 0;

        foreach (var split in transfer.Splits)
        {
            if (!split.JournalEntryLineId.HasValue || split.JournalEntryLineId == Guid.Empty)
                continue;

            if (!sourceLines.TryGetValue(split.JournalEntryLineId.Value, out var sourceLine))
                continue;

            ApplyJournalEntryLineContextToTransferSplit(split, sourceLine);

            if (escrowDepositAccountId > 0 && sourceLine.ChartOfAccountId == escrowDepositAccountId)
            {
                // Shared deposit escrow lines back multiple payment slices — do not stamp the full line net on every split.
                continue;
            }

            split.SourceJournalEntryLineAmount = sourceLine.Debit - sourceLine.Credit;

            if (escrowDepositAccountId > 0)
            {
                var escrowSourceAmount = await ResolveTransferEscrowDepositSourceAmountAsync(
                    transfer.OrganizationId,
                    sourceLine,
                    escrowDepositAccountId);
                if (escrowSourceAmount.HasValue)
                    split.SourceJournalEntryLineAmount = escrowSourceAmount.Value;
            }
        }
    }

    private async Task<decimal?> ResolveTransferEscrowDepositSourceAmountAsync(Guid organizationId, JournalEntryLine linkedSourceLine, int escrowDepositAccountId)
    {
        if (linkedSourceLine.ChartOfAccountId == escrowDepositAccountId)
            return linkedSourceLine.Debit - linkedSourceLine.Credit;

        if (linkedSourceLine.JournalEntryId == Guid.Empty)
            return null;

        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(linkedSourceLine.JournalEntryId, organizationId);
        if (journalEntry?.JournalEntryLines == null || journalEntry.JournalEntryLines.Count == 0)
            return null;

        var escrowLine = journalEntry.JournalEntryLines
            .FirstOrDefault(line => line.ChartOfAccountId == escrowDepositAccountId);
        if (escrowLine == null)
            return null;

        return escrowLine.Debit - escrowLine.Credit;
    }

    private async Task<Dictionary<Guid, JournalEntryLine>> LoadJournalEntryLinesByIdsAsync(IEnumerable<Guid> journalEntryLineIds)
    {
        var sourceLines = new Dictionary<Guid, JournalEntryLine>();

        foreach (var journalEntryLineId in journalEntryLineIds)
        {
            if (journalEntryLineId == Guid.Empty || sourceLines.ContainsKey(journalEntryLineId))
                continue;

            var sourceLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
            if (sourceLine != null)
                sourceLines[journalEntryLineId] = sourceLine;
        }

        return sourceLines;
    }

    private static void ApplyJournalEntryLineContextToDepositSplit(DepositSplit split, JournalEntryLine sourceLine)
    {
        split.PropertyId = NormalizeOptionalGuid(sourceLine.PropertyId);
        split.ReservationId = NormalizeOptionalGuid(sourceLine.ReservationId);
        split.ContactId = NormalizeOptionalGuid(sourceLine.ContactId);
    }

    private static void ApplyJournalEntryLineContextToTransferSplit(TransferSplit split, JournalEntryLine sourceLine)
    {
        // Deposit allocation sets payment-scoped context before rematch links a shared escrow line.
        if (!split.PropertyId.HasValue || split.PropertyId == Guid.Empty)
            split.PropertyId = NormalizeOptionalGuid(sourceLine.PropertyId);
        if (!split.ReservationId.HasValue || split.ReservationId == Guid.Empty)
            split.ReservationId = NormalizeOptionalGuid(sourceLine.ReservationId);
        if (!split.ContactId.HasValue || split.ContactId == Guid.Empty)
            split.ContactId = NormalizeOptionalGuid(sourceLine.ContactId);
    }

    public Task EnrichTransferSplitsForDisplayAsync(Transfer transfer)
        => EnrichTransferSplitsFromJournalEntryLinesAsync(transfer);

    private static void ApplyDepositHeaderContextFromSplits(Deposit deposit)
    {
        if (deposit.Splits == null || deposit.Splits.Count == 0)
            return;

        deposit.PropertyId ??= FirstSplitContextId(deposit.Splits, split => split.PropertyId);
    }

    private static void ApplyTransferHeaderContextFromSplits(Transfer transfer)
    {
        if (transfer.Splits == null || transfer.Splits.Count == 0)
            return;

        transfer.PropertyId ??= FirstSplitContextId(transfer.Splits, split => split.PropertyId);
    }

    private static Guid? FirstSplitContextId<TSplit>(IEnumerable<TSplit> splits, Func<TSplit, Guid?> selector)
        => splits
            .Select(selector)
            .FirstOrDefault(id => id.HasValue && id != Guid.Empty);

    private static Guid? NormalizeOptionalGuid(Guid? value)
        => value is { } id && id != Guid.Empty ? id : null;
    #endregion

    #region Check Each Deposit Split Link And Keep Replace Or Clear It
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
                && PaymentAccountingMonthIsOnOrBeforeDeposit(candidate.TransactionDate, deposit.DepositDate))
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

            if (split.JournalEntryLineId is { } existingLineId && existingLineId != Guid.Empty)
            {
                var existingLine = await GetJournalEntryLineByIdCachedAsync(existingLineId);
                trail?.Note(existingLine == null
                    ? $"Rematch dead line: {splitLabel} amount={split.Amount:0.00} line={existingLineId}"
                    : $"Rematch clear invalid line: {splitLabel} amount={split.Amount:0.00} line={existingLineId}");
                split.JournalEntryLineId = null;
            }

            var resolvedLineId = paymentLineCandidates.Count > 0
                ? await ResolveDepositSplitJournalEntryLineIdAsync(
                    deposit,
                    split,
                    paymentLineCandidates,
                    claimedLineIds,
                    assignedLineIds)
                : null;

            if (resolvedLineId.HasValue && resolvedLineId != Guid.Empty)
            {
                split.JournalEntryLineId = resolvedLineId;
                assignedLineIds.Add(resolvedLineId.Value);
                trail?.Note($"Rematch linked: {splitLabel} amount={split.Amount:0.00} -> line={resolvedLineId}");
                continue;
            }

            trail?.Bail($"Rematch failed: {splitLabel} amount={split.Amount:0.00} (no exact UF line match).");
        }
    }

    private async Task<bool> IsValidDepositSplitJournalEntryLineAsync(Deposit deposit, DepositSplit split, int undepositedFundsAccountId)
    {
        if (split.JournalEntryLineId is not { } journalEntryLineId || journalEntryLineId == Guid.Empty)
            return false;

        var line = await GetJournalEntryLineByIdCachedAsync(journalEntryLineId);
        if (line == null)
            return false;

        if (!await DepositSplitLineStillPointsAtPaymentAsync(deposit.OrganizationId, journalEntryLineId))
            return false;

        var paymentId = await ResolvePaymentIdFromDepositSplitLineAsync(split, deposit.OrganizationId);
        if (paymentId == Guid.Empty)
            return false;

        Payment? payment = null;
        if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
            payment = cachedPayment;
        else
            payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, deposit.OrganizationId);

        if (payment?.DepositId is { } stampedDepositId && stampedDepositId != Guid.Empty && stampedDepositId != deposit.DepositId)
            return false;

        var paymentJournalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, deposit.OrganizationId);
        if (paymentJournalEntry == null
            || !JournalEntryMatchesDepositAccountingMonth(deposit, payment?.PaymentDate))
            return false;

        var accountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId.Value : undepositedFundsAccountId;
        var lineNet = line.Debit - line.Credit;
        if (line.ChartOfAccountId == accountId)
        {
            if (!DepositSplitMatchesUndepositedLineAmount(split, lineNet))
                return false;
        }
        else if (!IsValidPaymentAllocationDepositSplitLinkLine(split, line, paymentJournalEntry))
            return false;

        if (!await SplitLineContextMatchesResolvedLineAsync(
                split.PropertyId,
                split.ReservationId,
                line,
                deposit.OrganizationId,
                BuildLinkContextHintsFromDepositSplit(split),
                paymentJournalEntry))
            return false;

        return true;
    }

    private static bool IsValidPaymentAllocationDepositSplitLinkLine(DepositSplit split, JournalEntryLine line, JournalEntry paymentJournalEntry)
    {
        if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentJournalEntry))
            return false;

        var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        var lineSourceCode = ResolveJournalEntryLineInvoiceSourceCode(line, paymentJournalEntry);
        if (string.IsNullOrWhiteSpace(splitSourceCode) || string.IsNullOrWhiteSpace(lineSourceCode))
            return false;

        if (!EntityCodeFormatting.CodesMatch(lineSourceCode, splitSourceCode))
            return false;

        return DepositSplitMatchesUndepositedLineAmount(split, line.Debit - line.Credit);
    }

    private async Task<bool> PaymentHasUndepositedFundsLineEqualToAmountAsync(Payment payment)
    {
        if (payment.PaymentId == Guid.Empty || payment.Amount == 0)
            return false;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(payment.OrganizationId, payment.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, payment.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            return false;

        var paymentAmount = Math.Abs(RoundCurrency(payment.Amount));
        var paymentEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(
            new JournalEntryGetByPaymentIdCriteria
            {
                OrganizationId = payment.OrganizationId,
                PaymentId = payment.PaymentId
            })).ToList();

        var undepositedFundsTotal = 0m;
        foreach (var paymentEntry in paymentEntries)
        {
            if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                continue;

            foreach (var line in paymentEntry.JournalEntryLines ?? [])
            {
                if (line.ChartOfAccountId != undepositedFundsAccountId || line.JournalEntryLineId == Guid.Empty)
                    continue;

                undepositedFundsTotal += line.Debit - line.Credit;
            }
        }

        return Math.Abs(undepositedFundsTotal - paymentAmount) <= 0.005m;
    }

    private Task ReconcileDepositSplitsForPaymentAsync(Payment payment, Guid currentUser)
        => ReconcileDepositSplitsForPaymentAsync(payment, currentUser, trail: null);

    private async Task<bool> UnlinkAmountMismatchedDepositSplitLinksForDepositHealthFixAsync(
        Deposit deposit,
        Guid currentUser,
        AccountingSyncBailTrail? trail)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            return false;

        var changed = false;
        foreach (var split in deposit.Splits ?? [])
        {
            if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                continue;

            if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                continue;

            var line = await GetJournalEntryLineByIdCachedAsync(lineId);
            if (line == null)
            {
                trail?.Note($"Step1 unlink: split {split.DepositSplitId} pointed at missing line {lineId}.");
                split.JournalEntryLineId = null;
                changed = true;
                continue;
            }

            var splitAmount = Math.Abs(RoundCurrency(split.Amount));
            var lineNet = Math.Abs(line.Debit - line.Credit);
            var paymentId = await ResolvePaymentIdFromDepositSplitLineAsync(split, deposit.OrganizationId);
            Payment? payment = null;
            if (paymentId != Guid.Empty)
            {
                if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                    payment = cachedPayment;
                else
                    payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, deposit.OrganizationId);
            }

            var amountMismatch = Math.Abs(lineNet - splitAmount) > 0.005m;
            var dateMismatch = payment != null && !JournalEntryMatchesDepositAccountingMonth(deposit, payment.PaymentDate);
            var contextMismatch = !await SplitLineContextMatchesResolvedLineAsync(
                split.PropertyId,
                split.ReservationId,
                line,
                deposit.OrganizationId,
                BuildLinkContextHintsFromDepositSplit(split));
            if (!amountMismatch && !dateMismatch && !contextMismatch)
                continue;

            trail?.Note(
                $"Step1 unlink invalid link: split {split.DepositSplitId} amount={splitAmount:0.00} line={lineId} lineAmount={lineNet:0.00} "
                + $"amountMismatch={amountMismatch} dateMismatch={dateMismatch} contextMismatch={contextMismatch}.");
            split.JournalEntryLineId = null;
            changed = true;
        }

        if (!changed)
            return false;

        deposit.ModifiedBy = currentUser;
        var updated = await _accountingRepository.UpdateDepositAsync(deposit);
        deposit.Splits = updated.Splits;
        _officeSyncCache?.ReplaceDeposit(deposit);
        return true;
    }

    private async Task<bool> PruneWrongDepositSplitLinksForPaymentHealthFixAsync(
        Payment payment,
        Deposit deposit,
        Guid currentUser,
        AccountingSyncBailTrail? trail)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(payment.OrganizationId, payment.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, payment.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            return false;

        var changed = false;
        foreach (var split in deposit.Splits ?? [])
        {
            if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                continue;

            if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                continue;

            var clearReason = await DescribeWrongDepositSplitLinkForPaymentHealthFixAsync(
                payment,
                deposit,
                split,
                undepositedFundsAccountId,
                lineId);
            if (clearReason == null)
                continue;

            trail?.Note($"Cleared wrong deposit split {split.DepositSplitId} link: {clearReason}");
            split.JournalEntryLineId = null;
            changed = true;
        }

        if (!changed)
            return false;

        deposit.ModifiedBy = currentUser;
        var updated = await _accountingRepository.UpdateDepositAsync(deposit);
        deposit.Splits = updated.Splits;
        _officeSyncCache?.ReplaceDeposit(deposit);
        return true;
    }

    private async Task<string?> DescribeWrongDepositSplitLinkForPaymentHealthFixAsync(
        Payment payment,
        Deposit deposit,
        DepositSplit split,
        int undepositedFundsAccountId,
        Guid lineId)
    {
        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        var paymentAmount = Math.Abs(RoundCurrency(payment.Amount));
        var splitTargetsThisPayment = Math.Abs(splitAmount - paymentAmount) <= 0.005m
            || (payment.LedgerLines ?? []).Any(line => Math.Abs(RoundCurrency(line.Amount) - splitAmount) <= 0.005m);

        var line = await GetJournalEntryLineByIdCachedAsync(lineId);
        if (line == null)
            return $"split {split.DepositSplitId} pointed at missing journal entry line {lineId}.";

        var journalEntry = await GetJournalEntryByIdCachedAsync(line.JournalEntryId, payment.OrganizationId);
        var journalEntryCode = string.IsNullOrWhiteSpace(journalEntry?.JournalEntryCode)
            ? line.JournalEntryId.ToString()
            : journalEntry.JournalEntryCode.Trim();

        if (journalEntry == null)
            return $"split {split.DepositSplitId} pointed at missing journal entry for line {lineId}.";

        if (journalEntry.PaymentId is { } linkedPaymentId
            && linkedPaymentId != Guid.Empty
            && linkedPaymentId == payment.PaymentId)
        {
            if (line.ChartOfAccountId != undepositedFundsAccountId)
                return $"split {split.DepositSplitId} linked to {journalEntryCode} on non-UF account.";

            if (!IsRematchableHealthInvoicePaymentJournalEntry(journalEntry))
                return $"split {split.DepositSplitId} linked to {journalEntryCode} which is not a payment/pre-payment-receive JE.";

            var lineNet = Math.Abs(line.Debit - line.Credit);
            if (Math.Abs(lineNet - splitAmount) > 0.005m)
                return $"split {split.DepositSplitId} linked to {journalEntryCode} amount {lineNet:0.00} does not match split {splitAmount:0.00}.";

            if (!JournalEntryMatchesDepositAccountingMonth(deposit, payment.PaymentDate))
                return $"split {split.DepositSplitId} linked to {journalEntryCode} but payment date is after deposit date.";

            if (!await SplitLineContextMatchesResolvedLineAsync(
                    split.PropertyId,
                    split.ReservationId,
                    line,
                    payment.OrganizationId,
                    BuildLinkContextHintsFromDepositSplit(split),
                    journalEntry))
                return $"split {split.DepositSplitId} linked to {journalEntryCode} property/reservation does not match split/invoice.";

            return null;
        }

        if (!splitTargetsThisPayment)
            return null;

        if (journalEntry.PaymentId is not { } otherPaymentId || otherPaymentId == Guid.Empty || otherPaymentId != payment.PaymentId)
        {
            return $"split {split.DepositSplitId} linked to {journalEntryCode} "
                + $"(PaymentId={journalEntry.PaymentId}) but belongs to payment {payment.PaymentCode}.";
        }

        return null;
    }

    private async Task ReconcileDepositSplitsForPaymentAsync(Payment payment, Guid currentUser, AccountingSyncBailTrail? trail)
    {
        if (payment.DepositId is not { } depositId || depositId == Guid.Empty)
        {
            trail?.Note("Exit: payment is not stamped to a deposit.");
            return;
        }

        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, payment.OrganizationId);
        if (deposit?.Splits == null || deposit.Splits.Count == 0)
        {
            trail?.Bail(deposit == null
                ? "Exit: stamped deposit record was not found."
                : "Exit: Deposit has no splits — open the deposit and resave.");
            return;
        }

        if (!PaymentAccountingMonthIsOnOrBeforeDeposit(payment.PaymentDate, deposit.DepositDate))
        {
            trail?.Bail($"Exit: Payment month is after deposit month (payment {payment.PaymentDate:yyyy-MM-dd}, deposit {deposit.DepositDate:yyyy-MM-dd}).");
            return;
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
        {
            trail?.Bail("Exit: No default Undeposited Funds account configured for office.");
            return;
        }

        var ufLines = new List<(Guid LineId, decimal Amount, JournalEntryLine Line, JournalEntry? JournalEntry)>();
        var paymentEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(
            new JournalEntryGetByPaymentIdCriteria
            {
                OrganizationId = deposit.OrganizationId,
                PaymentId = payment.PaymentId
            })).ToList();
        foreach (var paymentEntry in paymentEntries)
        {
            if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                continue;

            foreach (var line in paymentEntry.JournalEntryLines ?? [])
            {
                if (line.ChartOfAccountId != undepositedFundsAccountId || line.JournalEntryLineId == Guid.Empty)
                    continue;

                var netAmount = Math.Abs(line.Debit - line.Credit);
                if (netAmount <= 0.005m)
                    continue;

                ufLines.Add((
                    line.JournalEntryLineId,
                    netAmount,
                    line,
                    paymentEntry));
            }
        }

        if (ufLines.Count == 0)
        {
            trail?.Bail("Exit: No UF lines on payment journal entries (kind 13/14 with PaymentId).");
            return;
        }

        var claimedByOtherDeposits = await GetJournalEntryLineIdsClaimedByOtherDepositsAsync(deposit);
        var assignedLineIds = new HashSet<Guid>();
        var changed = false;
        foreach (var split in deposit.Splits)
        {
            if (Math.Abs(split.Amount) <= 0.005m || !IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
                continue;

            if (!DepositSplitTargetsPayment(split, payment))
                continue;

            var splitAmount = Math.Abs(RoundCurrency(split.Amount));
            if (split.JournalEntryLineId is { } existingLineId && existingLineId != Guid.Empty)
            {
                var existingLine = await GetJournalEntryLineByIdCachedAsync(existingLineId);
                if (existingLine != null)
                {
                    var existingNet = Math.Abs(existingLine.Debit - existingLine.Credit);
                    var existingJe = await GetJournalEntryByIdCachedAsync(existingLine.JournalEntryId, payment.OrganizationId);
                    if (existingJe?.PaymentId == payment.PaymentId
                        && Math.Abs(existingNet - splitAmount) <= 0.005m
                        && JournalEntryMatchesDepositAccountingMonth(deposit, payment.PaymentDate)
                        && await SplitLineContextMatchesResolvedLineAsync(
                            split.PropertyId,
                            split.ReservationId,
                            existingLine,
                            payment.OrganizationId,
                            BuildLinkContextHintsFromDepositSplit(split),
                            existingJe))
                    {
                        assignedLineIds.Add(existingLineId);
                        continue;
                    }
                }

                split.JournalEntryLineId = null;
                changed = true;
            }

            var amountMatches = ufLines
                .Where(line =>
                    Math.Abs(line.Amount - splitAmount) <= 0.005m
                    && !claimedByOtherDeposits.Contains(line.LineId)
                    && !assignedLineIds.Contains(line.LineId))
                .ToList();
            var exactLineMatches = new List<(Guid LineId, decimal Amount, JournalEntryLine Line, JournalEntry? JournalEntry)>();
            foreach (var candidate in amountMatches)
            {
                if (await SplitLineContextMatchesResolvedLineAsync(
                        split.PropertyId,
                        split.ReservationId,
                        candidate.Line,
                        payment.OrganizationId,
                        BuildLinkContextHintsFromDepositSplit(split),
                        candidate.JournalEntry))
                {
                    exactLineMatches.Add(candidate);
                }
            }

            if (exactLineMatches.Count != 1)
                continue;

            split.JournalEntryLineId = exactLineMatches[0].LineId;
            assignedLineIds.Add(exactLineMatches[0].LineId);
            changed = true;
        }

        if (!changed)
        {
            trail?.Bail("Exit: No deposit split changes applied — no exact split-to-UF-line match for this payment.");
            return;
        }

        deposit.ModifiedBy = currentUser;
        var updated = await _accountingRepository.UpdateDepositAsync(deposit);
        deposit.Splits = updated.Splits;
        _officeSyncCache?.ReplaceDeposit(deposit);
        trail?.Note("Applied deposit split link updates for payment UF line.");
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

    private static bool DepositSplitMatchesUndepositedLineAmount(DepositSplit split, decimal lineNetAmount)
        => Math.Abs(Math.Abs(lineNetAmount) - Math.Abs(RoundCurrency(split.Amount))) <= 0.005m;

    private static bool DepositSplitTargetsPayment(DepositSplit split, Payment payment)
    {
        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        var paymentAmount = Math.Abs(RoundCurrency(payment.Amount));
        if (Math.Abs(splitAmount - paymentAmount) <= 0.005m)
            return true;

        return (payment.LedgerLines ?? []).Any(line => Math.Abs(RoundCurrency(line.Amount) - splitAmount) <= 0.005m);
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

    private static bool DepositSplitReservationIdsChanged(IReadOnlyList<Guid?> originalReservationIds, IReadOnlyList<DepositSplit>? reconciledSplits)
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

    private static bool DepositSplitPropertyIdsChanged(IReadOnlyList<Guid?> originalPropertyIds, IReadOnlyList<DepositSplit>? reconciledSplits)
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

    private static bool DepositSplitReconciliationChanged(IReadOnlyList<Guid?> originalLineIds, IReadOnlyList<Guid?> originalReservationIds, IReadOnlyList<Guid?> originalPropertyIds, IReadOnlyList<DepositSplit>? reconciledSplits)
        => DepositSplitJournalEntryLineIdsChanged(originalLineIds, reconciledSplits)
            || DepositSplitReservationIdsChanged(originalReservationIds, reconciledSplits)
            || DepositSplitPropertyIdsChanged(originalPropertyIds, reconciledSplits);
    #endregion

    #region Find An Undeposited Funds Line Whose Amount Equals The Split
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

    private async Task<Guid> ResolveInvoiceIdBySourceCodeAsync(Guid organizationId, int officeId, string invoiceSourceCode)
    {
        var normalizedSourceCode = invoiceSourceCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSourceCode))
            return Guid.Empty;

        if (_officeSyncCache != null)
        {
            var cachedInvoice = _officeSyncCache.InvoicesById.Values.FirstOrDefault(invoice =>
                EntityCodeFormatting.CodesMatch(invoice.InvoiceCode, normalizedSourceCode));
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
            .FirstOrDefault(invoice => EntityCodeFormatting.CodesMatch(invoice.InvoiceCode, normalizedSourceCode))
            ?.InvoiceId ?? Guid.Empty;
    }

    private static bool IsRematchableHealthInvoicePaymentJournalEntry(JournalEntry entry)
        => entry.PaymentId is { } paymentId
            && paymentId != Guid.Empty
            && entry.JournalEntryKindId is JournalEntryKind.Payment or JournalEntryKind.PrePaymentReceive;

    private static bool IsPaymentLinkedJournalEntryForDepositUfRematch(JournalEntry entry)
        => IsRematchableHealthInvoicePaymentJournalEntry(entry);

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
                if (!IsPaymentLinkedJournalEntryForDepositUfRematch(paymentEntry))
                    continue;

                AppendUndepositedPaymentLineCandidates(
                    candidates,
                    paymentEntry,
                    undepositedFundsAccountId,
                    payment.DepositId,
                    payment.PaymentDate);
            }
        }

        return candidates;
    }

    private static void AppendUndepositedPaymentLineCandidates(ICollection<UndepositedPaymentLineCandidate> candidates, JournalEntry paymentEntry, int undepositedFundsAccountId, Guid? paymentDepositId, DateOnly paymentDate)
    {
        if (paymentDate == default || !IsPaymentLinkedJournalEntryForDepositUfRematch(paymentEntry))
            return;

        var paymentSourceCode = ResolvePaymentJournalEntrySourceCode(paymentEntry);

        foreach (var line in paymentEntry.JournalEntryLines ?? [])
        {
            var netAmount = line.Debit - line.Credit;
            if (Math.Abs(netAmount) <= 0.005m)
                continue;

            var isUndepositedFundsLine = line.ChartOfAccountId == undepositedFundsAccountId;
            var sourceCode = isUndepositedFundsLine
                ? paymentSourceCode
                : ResolveJournalEntryLineInvoiceSourceCode(line, paymentEntry);

            if (string.IsNullOrWhiteSpace(sourceCode))
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
                TransactionDate = paymentDate,
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

    private static bool IsPaymentDepositStampAvailableForDeposit(Guid? paymentDepositId, Guid depositId)
    {
        if (paymentDepositId is not { } stampedDepositId || stampedDepositId == Guid.Empty)
            return true;

        return stampedDepositId == depositId;
    }

    private async Task<DateOnly> GetDocumentPaymentDateAsync(Guid paymentId, Guid organizationId)
    {
        if (paymentId == Guid.Empty)
            return default;

        if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
            return cachedPayment.PaymentDate;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        return payment?.PaymentDate ?? default;
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

    private async Task<HashSet<Guid>> FilterPaymentIdsAvailableForDepositRematchAsync(Deposit deposit, IReadOnlyCollection<Guid> paymentIds)
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

    private async Task<Guid?> ResolveDepositSplitJournalEntryLineIdAsync(
        Deposit deposit,
        DepositSplit split,
        IReadOnlyList<UndepositedPaymentLineCandidate> candidates,
        IReadOnlySet<Guid> claimedLineIds,
        IReadOnlySet<Guid> assignedLineIds)
    {
        var splitAmount = Math.Abs(RoundCurrency(split.Amount));
        if (splitAmount <= 0.005m)
            return null;

        var splitSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
        var hints = BuildLinkContextHintsFromDepositSplit(split);

        var amountMatches = candidates
            .Where(candidate =>
                IsPaymentDepositStampAvailableForDeposit(candidate.DepositId, deposit.DepositId)
                && !claimedLineIds.Contains(candidate.JournalEntryLineId)
                && !assignedLineIds.Contains(candidate.JournalEntryLineId)
                && Math.Abs(Math.Abs(candidate.NetAmount) - splitAmount) <= 0.005m
                && PaymentAccountingMonthIsOnOrBeforeDeposit(candidate.TransactionDate, deposit.DepositDate)
                && (string.IsNullOrWhiteSpace(splitSourceCode)
                    || EntityCodeFormatting.CodesMatch(candidate.SourceCode, splitSourceCode)))
            .ToList();

        var contextMatches = new List<UndepositedPaymentLineCandidate>();
        foreach (var candidate in amountMatches)
        {
            var line = await GetJournalEntryLineByIdCachedAsync(candidate.JournalEntryLineId);
            if (line == null)
                continue;

            if (await SplitLineContextMatchesResolvedLineAsync(
                    split.PropertyId,
                    split.ReservationId,
                    line,
                    deposit.OrganizationId,
                    hints))
            {
                contextMatches.Add(candidate);
            }
        }

        return contextMatches.Count == 1 ? contextMatches[0].JournalEntryLineId : null;
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

    private static string ResolveJournalEntryLineInvoiceSourceCode(JournalEntryLine line, JournalEntry paymentEntry)
    {
        var memo = CoalesceJournalEntryMemo(line.Memo, paymentEntry.Memo);
        if (string.IsNullOrWhiteSpace(memo))
            return string.Empty;

        var paymentMemoMatch = MatchPaymentMemo(memo);
        if (paymentMemoMatch.IsMatch && !string.IsNullOrWhiteSpace(paymentMemoMatch.SourceCode))
            return paymentMemoMatch.SourceCode.Trim();

        var prepaymentMemoMatch = MatchPrePaymentMemo(memo);
        if (prepaymentMemoMatch.IsMatch && !string.IsNullOrWhiteSpace(prepaymentMemoMatch.SourceCode))
            return prepaymentMemoMatch.SourceCode.Trim();

        if (TryParseInvoiceSourceCodeFromMemo(memo, out var invoiceSourceCode))
            return invoiceSourceCode;

        var colonIndex = memo.IndexOf(':');
        return colonIndex > 0 ? memo[..colonIndex].Trim() : string.Empty;
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

    private static bool GuidEquals(Guid? left, Guid? right)
    {
        var normalizedLeft = NormalizeOptionalGuid(left);
        var normalizedRight = NormalizeOptionalGuid(right);
        return normalizedLeft != null && normalizedRight != null && normalizedLeft == normalizedRight;
    }
    #endregion

    #region Fill A Missing Reservation Or Property On A Deposit Split
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

    private async Task<DepositSplitReservationContext?> ResolveDepositSplitReservationContextAsync(Deposit deposit, DepositSplit split)
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
                if (!EntityCodeFormatting.CodesMatch(invoice.ReservationCode, reservationSourceCode))
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
            EntityCodeFormatting.CodesMatch(reservation.ReservationCode, reservationSourceCode));
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
            EntityCodeFormatting.CodesMatch(reservation.ReservationCode, reservationSourceCode));
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
    #endregion

    #region Health Fix — unlink invalid deposited payments
    private static bool PaymentTransactionDateIsAfterDepositDate(Payment payment, Deposit deposit)
        => payment.PaymentDate > deposit.DepositDate;

    private async Task<bool> TryUnlinkInvoicePaymentFromInvalidDepositForHealthFixAsync(
        Payment payment,
        Guid organizationId,
        Guid currentUser,
        JournalEntrySyncResult result)
    {
        if (payment.DepositId is not { } depositId || depositId == Guid.Empty)
            return false;

        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
        {
            await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
            return true;
        }

        if (!PaymentTransactionDateIsAfterDepositDate(payment, deposit))
            return false;

        await UnlinkInvoicePaymentFromDepositAsync(payment, deposit, organizationId, currentUser);
        result.JournalEntriesSkipped++;
        return true;
    }

    private async Task UnlinkInvoicePaymentFromDepositAsync(
        Payment payment,
        Deposit deposit,
        Guid organizationId,
        Guid currentUser)
    {
        var paymentJournalEntries = (await GetJournalEntriesByPaymentIdCachedAsync(organizationId, payment.PaymentId)).ToList();
        if (paymentJournalEntries.Count == 0)
        {
            paymentJournalEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(
                new JournalEntryGetByPaymentIdCriteria
                {
                    OrganizationId = organizationId,
                    PaymentId = payment.PaymentId
                })).ToList();
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, payment.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, payment.OfficeId, accountingOffice);
        var paymentUndepositedFundsLineIds = CollectPaymentUndepositedFundsLineIds(paymentJournalEntries, undepositedFundsAccountId);

        var depositChanged = false;
        foreach (var split in deposit.Splits ?? [])
        {
            if (!await DepositSplitLinksToInvoicePaymentAsync(
                    split,
                    payment,
                    undepositedFundsAccountId,
                    paymentUndepositedFundsLineIds,
                    organizationId))
            {
                continue;
            }

            split.JournalEntryLineId = null;
            depositChanged = true;
        }

        if (depositChanged)
        {
            deposit.ModifiedBy = currentUser;
            var updatedDeposit = await _accountingRepository.UpdateDepositAsync(deposit);
            deposit.Splits = updatedDeposit.Splits;
            _officeSyncCache?.ReplaceDeposit(deposit);
        }

        foreach (var journalEntry in paymentJournalEntries)
            await TryClearJournalEntryDepositDocumentLinkAsync(journalEntry, currentUser);

        var invoiceChargeJournalEntries = await LoadDepositInvoiceChargeJournalEntriesForPaymentJournalEntriesAsync(
            organizationId,
            payment.OfficeId,
            paymentJournalEntries);
        foreach (var journalEntry in invoiceChargeJournalEntries)
            await TryClearJournalEntryDepositDocumentLinkAsync(journalEntry, currentUser);

        foreach (var paymentLine in payment.LedgerLines.Where(line => line.Amount != 0))
        {
            if (paymentLine.InvoiceId == Guid.Empty)
                continue;

            Invoice? invoice = null;
            if (_officeSyncCache == null
                || !_officeSyncCache.TryGetInvoiceWithLedgerLines(paymentLine.InvoiceId, out invoice))
            {
                invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, organizationId);
            }

            if (invoice == null)
                continue;

            var paymentLineJournalEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                invoice,
                ToInvoicePaymentLedgerLine(paymentLine));
            foreach (var journalEntry in paymentLineJournalEntries)
                await TryClearJournalEntryDepositDocumentLinkAsync(journalEntry, currentUser);
        }

        await ClearInvoicePaymentDepositStampAsync(payment, organizationId, currentUser);
    }

    private static HashSet<Guid> CollectPaymentUndepositedFundsLineIds(
        IReadOnlyList<JournalEntry> paymentJournalEntries,
        int undepositedFundsAccountId)
    {
        var lineIds = new HashSet<Guid>();
        if (undepositedFundsAccountId <= 0)
            return lineIds;

        foreach (var journalEntry in paymentJournalEntries)
        {
            foreach (var line in journalEntry.JournalEntryLines ?? [])
            {
                if (line.JournalEntryLineId != Guid.Empty
                    && line.ChartOfAccountId == undepositedFundsAccountId
                    && Math.Abs(line.Debit - line.Credit) > 0.005m)
                {
                    lineIds.Add(line.JournalEntryLineId);
                }
            }
        }

        return lineIds;
    }

    private async Task<bool> DepositSplitLinksToInvoicePaymentAsync(
        DepositSplit split,
        Payment payment,
        int undepositedFundsAccountId,
        IReadOnlySet<Guid> paymentUndepositedFundsLineIds,
        Guid organizationId)
    {
        if (!IsPaymentBackedDepositSplit(split, undepositedFundsAccountId))
            return false;

        if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
            return false;

        if (paymentUndepositedFundsLineIds.Contains(lineId))
            return true;

        if (await ResolvePaymentIdFromDepositSplitLineAsync(split, organizationId) == payment.PaymentId)
            return true;

        var sourceLine = await GetJournalEntryLineByIdCachedAsync(lineId);
        if (sourceLine?.JournalEntryId is not { } journalEntryId || journalEntryId == Guid.Empty)
            return false;

        var journalEntry = await GetJournalEntryByIdCachedAsync(journalEntryId, organizationId);
        return journalEntry?.PaymentId == payment.PaymentId;
    }

    private async Task ClearInvoicePaymentDepositStampAsync(Payment payment, Guid organizationId, Guid currentUser)
    {
        await _accountingRepository.SetPaymentDepositIdAsync(payment.PaymentId, organizationId, null, currentUser);

        payment.DepositId = null;
        payment.DepositCode = string.Empty;

        if (_officeSyncCache?.PaymentsById.TryGetValue(payment.PaymentId, out var cachedPayment) == true)
        {
            cachedPayment.DepositId = null;
            cachedPayment.DepositCode = string.Empty;
        }

        var reloadedPayment = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, organizationId);
        if (reloadedPayment?.DepositId is { } remainingDepositId && remainingDepositId != Guid.Empty)
            throw new Exception($"Payment {payment.PaymentCode} still stamped to deposit after unlink.");
    }
    #endregion
}

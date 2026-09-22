using RentAll.Domain.Accounting;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Types

    private class TransferDepositRecapAccountContext
    {
        public int AccountsReceivableAccountId { get; init; }
        public int UndepositedFundsAccountId { get; init; }
        public int PrePaymentAccountId { get; init; }
        public int OwnerAccountsPayableAccountId { get; init; }
        public int OwnerExpenseAccountId { get; init; }
        public int TenantIncomeAccountId { get; init; }
        public int EscrowSecDepAccountId { get; init; }
        public int EscrowSdwAccountId { get; init; }
        public int EscrowDepositAccountId { get; init; }
        public HashSet<int> RentalIncomeAccountIds { get; init; } = [];
    }

    private class TransferDepositAllocationScope
    {
        public Guid PaymentId { get; init; }
        public Guid? InvoiceId { get; init; }
        public string SourceCode { get; init; } = string.Empty;
        public string PaymentMemoSourceCode { get; init; } = string.Empty;
        public decimal SplitAmount { get; init; }
        public Guid? PropertyId { get; init; }
        public Guid? ReservationId { get; init; }
        public Guid? ContactId { get; init; }
    }

    #endregion

    #region Resolve Transfer Deposit Allocations

    public async Task<IReadOnlyList<TransferDepositAllocationResult>> ResolveTransferDepositAllocationsAsync(Guid organizationId, int officeId, IReadOnlyList<TransferDepositAllocationRequestItem> items)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        if (officeId <= 0)
            throw new ArgumentException("OfficeId is required.", nameof(officeId));

        if (items == null || items.Count == 0)
            return [];

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var recapContext = BuildTransferDepositRecapAccountContext(chartOfAccounts, officeId, accountingOffice);
        var results = new List<TransferDepositAllocationResult>();

        var expandedItems = await ExpandTransferDepositAllocationItemsAsync(organizationId, items);
        foreach (var item in expandedItems)
        {
            if (item.DepositId == Guid.Empty)
                throw new InvalidOperationException("DepositId is required for each transfer deposit allocation item.");

            results.Add(await ResolveTransferDepositAllocationAsync(
                organizationId,
                officeId,
                item.DepositId,
                item.EscrowAmount,
                recapContext,
                item.JournalEntryLineId,
                item.DepositSplitId));
        }

        return results;
    }

    private async Task<IReadOnlyList<TransferDepositAllocationRequestItem>> ExpandTransferDepositAllocationItemsAsync(Guid organizationId, IReadOnlyList<TransferDepositAllocationRequestItem> items)
    {
        var expandedItems = new List<TransferDepositAllocationRequestItem>();

        foreach (var item in items)
        {
            if (item.DepositId == Guid.Empty)
                continue;

            var deposit = await _accountingRepository.GetDepositByIdAsync(item.DepositId, organizationId)
                ?? throw new InvalidOperationException($"Deposit {item.DepositId} was not found.");

            if (deposit.Splits == null || deposit.Splits.Count == 0)
                throw new InvalidOperationException($"Deposit {deposit.DepositCode} has no splits.");

            if (deposit.Splits.Count == 1)
            {
                var singleSplit = deposit.Splits[0];
                expandedItems.Add(new TransferDepositAllocationRequestItem
                {
                    DepositId = item.DepositId,
                    EscrowAmount = Math.Abs(RoundCurrency(item.EscrowAmount)) > 0.005m
                        ? NormalizeTransferEscrowAmount(item.EscrowAmount)
                        : NormalizeTransferEscrowAmount(singleSplit.Amount),
                    JournalEntryLineId = item.JournalEntryLineId,
                    DepositSplitId = singleSplit.DepositSplitId > 0 ? singleSplit.DepositSplitId : item.DepositSplitId
                });
                continue;
            }

            var normalizedAmount = RoundCurrency(Math.Abs(item.EscrowAmount));
            var depositAmount = RoundCurrency(Math.Abs(deposit.Amount));
            var splitTotal = RoundCurrency(deposit.Splits
                .Where(split => Math.Abs(split.Amount) > 0.005m)
                .Sum(split => Math.Abs(split.Amount)));
            var matchesFullDepositAmount = normalizedAmount != 0
                && (Math.Abs(normalizedAmount - depositAmount) <= 0.005m
                    || (splitTotal > 0 && Math.Abs(normalizedAmount - splitTotal) <= 0.005m));
            if (!matchesFullDepositAmount)
            {
                expandedItems.Add(new TransferDepositAllocationRequestItem
                {
                    DepositId = item.DepositId,
                    EscrowAmount = NormalizeTransferEscrowAmount(item.EscrowAmount),
                    JournalEntryLineId = item.JournalEntryLineId,
                    DepositSplitId = item.DepositSplitId
                });
                continue;
            }

            foreach (var split in deposit.Splits.Where(split => Math.Abs(split.Amount) > 0.005m))
            {
                expandedItems.Add(new TransferDepositAllocationRequestItem
                {
                    DepositId = item.DepositId,
                    EscrowAmount = NormalizeTransferEscrowAmount(split.Amount),
                    JournalEntryLineId = item.JournalEntryLineId,
                    DepositSplitId = split.DepositSplitId > 0 ? split.DepositSplitId : null
                });
            }
        }

        return expandedItems;
    }

    #endregion

    #region Resolve Single Deposit Allocation

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationForEscrowLineAsync(Guid organizationId, int officeId, Deposit deposit, decimal escrowAmount, TransferDepositRecapAccountContext recapContext, Guid? escrowJournalEntryLineId, int undepositedFundsAccountId, Dictionary<Guid, List<JournalEntry>> depositJournalEntryCache)
    {
        escrowAmount = NormalizeTransferEscrowAmount(escrowAmount);
        var expandedSplits = ExpandEscrowDepositSplits(deposit, escrowAmount);
        if (expandedSplits.Count <= 1)
        {
            var preferredSplit = expandedSplits.Count == 1 ? expandedSplits[0] : null;
            var singleAmount = preferredSplit != null ? preferredSplit.Amount : escrowAmount;
            return await ResolveTransferDepositAllocationAsync(
                organizationId,
                officeId,
                deposit,
                singleAmount,
                recapContext,
                escrowJournalEntryLineId,
                undepositedFundsAccountId,
                depositJournalEntryCache,
                preferredSplit?.DepositSplitId > 0 ? preferredSplit.DepositSplitId : null);
        }

        var ownerEscrow = 0m;
        var secDep = 0m;
        var sdw = 0m;
        Guid? propertyId = null;
        Guid? reservationId = null;
        Guid? contactId = null;
        var description = string.Empty;

        foreach (var split in expandedSplits)
        {
            var part = await ResolveTransferDepositAllocationAsync(
                organizationId,
                officeId,
                deposit,
                split.Amount,
                recapContext,
                null,
                undepositedFundsAccountId,
                depositJournalEntryCache,
                split.DepositSplitId > 0 ? split.DepositSplitId : null);

            ownerEscrow = RoundCurrency(ownerEscrow + part.OwnerEscrow);
            secDep = RoundCurrency(secDep + part.SecDep);
            sdw = RoundCurrency(sdw + part.Sdw);
            propertyId ??= part.PropertyId;
            reservationId ??= part.ReservationId;
            contactId ??= part.ContactId;
            if (string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(part.Description))
                description = part.Description;
        }

        var normalizedEscrowAmount = NormalizeTransferEscrowAmount(escrowAmount);
        // Business is always the deposit residual (company rent + fees). FeesActual is not the Business split amount.
        var business = RoundCurrency(normalizedEscrowAmount - ownerEscrow - secDep - sdw);

        return new TransferDepositAllocationResult
        {
            DepositId = deposit.DepositId,
            JournalEntryLineId = escrowJournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = normalizedEscrowAmount,
            OwnerEscrow = ownerEscrow,
            SecDep = secDep,
            Sdw = sdw,
            Business = business,
            PropertyId = propertyId,
            ReservationId = reservationId,
            ContactId = contactId,
            Description = description
        };
    }

    private static List<DepositSplit> ExpandEscrowDepositSplits(Deposit deposit, decimal escrowAmount)
    {
        var splits = (deposit.Splits ?? [])
            .Where(split => Math.Abs(split.Amount) > 0.005m)
            .ToList();

        if (splits.Count == 0)
            return [];

        if (splits.Count == 1)
            return splits;

        var normalizedAmount = RoundCurrency(Math.Abs(escrowAmount));
        var depositAmount = RoundCurrency(Math.Abs(deposit.Amount));
        var splitTotal = RoundCurrency(splits.Sum(split => Math.Abs(split.Amount)));
        var matchesFullDepositAmount = normalizedAmount != 0
            && (Math.Abs(normalizedAmount - depositAmount) <= 0.005m
                || (splitTotal > 0 && Math.Abs(normalizedAmount - splitTotal) <= 0.005m));

        if (matchesFullDepositAmount)
            return splits;

        return [];
    }

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationAsync(Guid organizationId, int officeId, Guid depositId, decimal escrowAmount, TransferDepositRecapAccountContext recapContext, Guid? escrowJournalEntryLineId = null, int? depositSplitId = null)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId)
            ?? throw new InvalidOperationException($"Deposit {depositId} was not found.");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            throw new InvalidOperationException($"Undeposited funds account is not configured for office {deposit.OfficeId}.");

        return await ResolveTransferDepositAllocationAsync(
            organizationId,
            officeId,
            deposit,
            escrowAmount,
            recapContext,
            escrowJournalEntryLineId,
            undepositedFundsAccountId,
            depositJournalEntryCache: null,
            depositSplitId);
    }

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationAsync(Guid organizationId, int officeId, Deposit deposit, decimal escrowAmount, TransferDepositRecapAccountContext recapContext, Guid? escrowJournalEntryLineId, int undepositedFundsAccountId, Dictionary<Guid, List<JournalEntry>>? depositJournalEntryCache, int? depositSplitId = null)
    {
        escrowAmount = NormalizeTransferEscrowAmount(escrowAmount);
        var depositId = deposit.DepositId;
        var matchedSplit = RequireTransferDepositSplit(deposit, escrowAmount, depositSplitId);

        if (!IsPaymentBackedDepositSplit(matchedSplit, undepositedFundsAccountId))
            return BuildNonPaymentTransferDepositAllocationResult(depositId, escrowJournalEntryLineId, escrowAmount, deposit, matchedSplit);

        if (!await IsValidDepositSplitJournalEntryLineAsync(deposit, matchedSplit, undepositedFundsAccountId))
        {
            var originalSplitLineIds = (deposit.Splits ?? [])
                .Select(split => split.JournalEntryLineId)
                .ToList();

            await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit);

            if (DepositSplitJournalEntryLineIdsChanged(originalSplitLineIds, deposit.Splits))
                deposit = await _accountingRepository.UpdateDepositAsync(deposit);

            matchedSplit = RequireTransferDepositSplit(deposit, escrowAmount, depositSplitId ?? matchedSplit.DepositSplitId);
        }

        var allocationScope = await RequireTransferDepositAllocationScopeAsync(organizationId, deposit, matchedSplit, undepositedFundsAccountId);

        List<JournalEntry> depositJournalEntries;
        if (depositJournalEntryCache != null && depositJournalEntryCache.TryGetValue(depositId, out var cachedEntries))
        {
            depositJournalEntries = cachedEntries;
        }
        else
        {
            depositJournalEntries = (await _journalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria
            {
                OrganizationId = organizationId,
                DepositId = depositId
            })).ToList();

            depositJournalEntryCache?.Add(depositId, depositJournalEntries);
        }

        depositJournalEntries = await MergeTransferDepositInvoiceChargeJournalEntriesAsync(organizationId, deposit.OfficeId, depositJournalEntries, allocationScope);

        var scopedDepositJournalEntries = FilterDepositLinkedJournalEntriesForScope(depositJournalEntries, allocationScope);
        if (scopedDepositJournalEntries.Count == 0)
        {
            throw new InvalidOperationException(
                $"No deposit-linked journal entries matched payment {allocationScope.SourceCode} for deposit {deposit.DepositCode}.");
        }

        var (ownerEscrow, secDep, sdw, description) =
            ClassifyTransferDepositAllocation(scopedDepositJournalEntries, allocationScope, recapContext);

        return BuildTransferDepositAllocationResult(
            depositId,
            escrowJournalEntryLineId,
            escrowAmount,
            allocationScope,
            ownerEscrow,
            secDep,
            sdw,
            description);
    }

    #endregion

    #region Non-Payment Deposit Allocation

    private static bool IsPaymentBackedDepositSplit(DepositSplit split, int undepositedFundsAccountId)
    {
        if (split.ChartOfAccountId is not > 0 || split.ChartOfAccountId != undepositedFundsAccountId)
            return false;

        var description = (split.Description ?? string.Empty).Trim();
        return !OfficeOpeningBalanceSheetMemoPattern.IsMatch(description);
    }

    private static TransferDepositAllocationResult BuildNonPaymentTransferDepositAllocationResult(Guid depositId, Guid? escrowJournalEntryLineId, decimal escrowAmount, Deposit deposit, DepositSplit split)
    {
        var normalizedEscrowAmount = NormalizeTransferEscrowAmount(escrowAmount);
        var description = ResolveTransferDepositAllocationDescription(split.Description, deposit.DepositCode);

        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
            JournalEntryLineId = escrowJournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = normalizedEscrowAmount,
            OwnerEscrow = 0m,
            SecDep = 0m,
            Sdw = 0m,
            Business = normalizedEscrowAmount,
            PropertyId = split.PropertyId,
            ReservationId = split.ReservationId,
            ContactId = split.ContactId,
            Description = description
        };
    }

    #endregion

    #region Resolve Transfer Report Line Allocations

    public async Task<IReadOnlyList<TransferReportLineAllocationResult>> ResolveTransferReportLineAllocationsAsync(Guid organizationId, Guid transferId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        if (transferId == Guid.Empty)
            throw new ArgumentException("TransferId is required.", nameof(transferId));

        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new InvalidOperationException("Transfer not found");

        await EnrichTransferSplitsFromJournalEntryLinesAsync(transfer);

        var (_, accountingOffice) = await LoadAccountContextAsync(organizationId, transfer.OfficeId);
        var ownersAccountId = accountingOffice?.DefaultEscrowOwnersAccountId;
        var secDepAccountId = accountingOffice?.DefaultEscrowSecDepAccountId;
        var sdwAccountId = accountingOffice?.DefaultEscrowSdwAccountId;
        var businessAccountId = accountingOffice?.DefaultBankAccountId;

        var results = new List<TransferReportLineAllocationResult>();

        var linkedGroups = (transfer.Splits ?? [])
            .Where(split => Math.Abs(split.Amount) > 0.005m
                && split.JournalEntryLineId is { } lineId && lineId != Guid.Empty)
            .GroupBy(split => split.JournalEntryLineId!.Value);

        foreach (var group in linkedGroups)
        {
            results.Add(BuildTransferReportLineAllocationFromSplits(
                group.ToList(),
                group.Key,
                ownersAccountId,
                secDepAccountId,
                sdwAccountId,
                businessAccountId));
        }

        foreach (var manualGroup in GroupUnlinkedTransferSplitsForReport(transfer.Splits))
        {
            results.Add(BuildTransferReportLineAllocationFromSplits(
                manualGroup,
                Guid.Empty,
                ownersAccountId,
                secDepAccountId,
                sdwAccountId,
                businessAccountId));
        }

        return results;
    }

    private static IEnumerable<List<TransferSplit>> GroupUnlinkedTransferSplitsForReport(IReadOnlyList<TransferSplit>? splits)
    {
        if (splits == null || splits.Count == 0)
            return [];

        return splits
            .Select((split, index) => (split, index))
            .Where(item => Math.Abs(item.split.Amount) > 0.005m
                && (item.split.JournalEntryLineId is null || item.split.JournalEntryLineId == Guid.Empty))
            .GroupBy(
                item => string.IsNullOrWhiteSpace(item.split.Description)
                    ? $"split:{item.index}"
                    : item.split.Description.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Select(item => item.split).ToList());
    }

    private static TransferReportLineAllocationResult BuildTransferReportLineAllocationFromSplits(
        IReadOnlyList<TransferSplit> splits,
        Guid journalEntryLineId,
        int? ownersAccountId,
        int? secDepAccountId,
        int? sdwAccountId,
        int? businessAccountId)
    {
        var contextSplit = splits[0];
        var description = splits
            .Select(split => (split.Description ?? string.Empty).Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? string.Empty;
        var ownerEscrow = SumTransferReportSplitAmountsForAccount(splits, ownersAccountId);
        var secDep = SumTransferReportSplitAmountsForAccount(splits, secDepAccountId);
        var sdw = SumTransferReportSplitAmountsForAccount(splits, sdwAccountId);
        var business = SumTransferReportSplitAmountsForAccount(splits, businessAccountId);
        var destinationTotal = RoundCurrency(ownerEscrow + secDep + sdw + business);

        return new TransferReportLineAllocationResult
        {
            JournalEntryLineId = journalEntryLineId,
            DepositId = Guid.Empty,
            EscrowAmount = ResolveTransferReportEscrowSourceAmount(splits, destinationTotal),
            OwnerEscrow = ownerEscrow,
            SecDep = secDep,
            Sdw = sdw,
            Business = business,
            PropertyId = contextSplit.PropertyId,
            ReservationId = contextSplit.ReservationId,
            ContactId = contextSplit.ContactId,
            Description = description
        };
    }

    private static decimal ResolveTransferReportEscrowSourceAmount(IReadOnlyList<TransferSplit> splits, decimal destinationTotal)
    {
        var sourceAmount = splits
            .Select(split => split.SourceJournalEntryLineAmount)
            .FirstOrDefault(value => value.HasValue && Math.Abs(value.Value) > 0.005m);
        if (sourceAmount.HasValue)
            return RoundCurrency(Math.Abs(sourceAmount.Value));

        return destinationTotal;
    }

    private static decimal SumTransferReportSplitAmountsForAccount(IReadOnlyList<TransferSplit> splits, int? accountId)
    {
        if (accountId is null or <= 0)
            return 0m;

        return RoundCurrency(splits
            .Where(split => split.ChartOfAccountId == accountId)
            .Sum(split => split.Amount));
    }

    #endregion

    #region Deposit Split Identification

    private static DepositSplit RequireTransferDepositSplit(Deposit deposit, decimal escrowAmount, int? depositSplitId = null)
    {
        var splits = (deposit.Splits ?? [])
            .Where(split => Math.Abs(split.Amount) > 0.005m)
            .ToList();

        if (splits.Count == 0)
            throw new InvalidOperationException($"Deposit {deposit.DepositCode} has no non-zero splits.");

        // Prefer the explicit deposit split (full-deposit expand / repaired identity).
        if (depositSplitId is > 0)
        {
            var byId = splits.FirstOrDefault(split => split.DepositSplitId == depositSplitId.Value);
            if (byId != null)
                return byId;
        }

        // Deposit splits link to UF payment lines — disambiguate by invoice + property + amount.
        var normalizedEscrowAmount = RoundCurrency(Math.Abs(escrowAmount));
        if (normalizedEscrowAmount != 0)
        {
            var amountMatches = splits
                .Where(split => Math.Abs(Math.Abs(split.Amount) - normalizedEscrowAmount) <= 0.005m)
                .ToList();

            if (amountMatches.Count == 1)
                return amountMatches[0];

            if (amountMatches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Deposit {deposit.DepositCode} has multiple splits matching escrow amount {normalizedEscrowAmount:0.00}.");
            }
        }

        if (splits.Count == 1)
            return splits[0];

        throw new InvalidOperationException(
            $"Could not identify a deposit split for deposit {deposit.DepositCode} and escrow amount {escrowAmount:0.00}.");
    }

    #endregion

    #region Payment Scope Resolution

    private async Task<TransferDepositAllocationScope> RequireTransferDepositAllocationScopeAsync(Guid organizationId, Deposit deposit, DepositSplit split, int undepositedFundsAccountId)
    {
        if (deposit.OfficeId <= 0)
            throw new InvalidOperationException($"Deposit {deposit.DepositCode} is missing OfficeId.");

        if (split.JournalEntryLineId is not { } paymentLineId || paymentLineId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Deposit split for {deposit.DepositCode} is missing the undeposited payment JournalEntryLineId.");
        }

        if (!await IsValidDepositSplitJournalEntryLineAsync(deposit, split, undepositedFundsAccountId))
        {
            throw new InvalidOperationException(
                $"Deposit split JournalEntryLineId {paymentLineId} is not a valid undeposited payment line for deposit {deposit.DepositCode}.");
        }

        var paymentLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(paymentLineId)
            ?? throw new InvalidOperationException($"Journal entry line {paymentLineId} was not found.");

        var paymentJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(paymentLine.JournalEntryId, organizationId)
            ?? throw new InvalidOperationException($"Journal entry {paymentLine.JournalEntryId} was not found.");

        if (paymentJournalEntry.PaymentId is not { } paymentId || paymentId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Payment journal entry {paymentJournalEntry.JournalEntryCode} is missing PaymentId for deposit {deposit.DepositCode}.");
        }

        if (string.IsNullOrWhiteSpace(paymentJournalEntry.SourceCode))
        {
            throw new InvalidOperationException(
                $"Payment journal entry {paymentJournalEntry.JournalEntryCode} is missing SourceCode for deposit {deposit.DepositCode}.");
        }

        Guid? invoiceId = paymentJournalEntry.SourceTypeId == (int)SourceType.Invoice
            && paymentJournalEntry.SourceId is { } sourceInvoiceId
            && sourceInvoiceId != Guid.Empty
            ? sourceInvoiceId
            : null;

        return new TransferDepositAllocationScope
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            SourceCode = paymentJournalEntry.SourceCode.Trim(),
            PaymentMemoSourceCode = ResolveTransferDepositPaymentMemoSourceCode(paymentJournalEntry, paymentLine),
            SplitAmount = split.Amount,
            PropertyId = split.PropertyId,
            ReservationId = split.ReservationId,
            ContactId = split.ContactId
        };
    }

    #endregion

    #region Deposit-Linked Journal Entry Selection

    private async Task<List<JournalEntry>> MergeTransferDepositInvoiceChargeJournalEntriesAsync(Guid organizationId, int officeId, IReadOnlyList<JournalEntry> depositJournalEntries, TransferDepositAllocationScope allocationScope)
    {
        var mergedEntries = depositJournalEntries.ToList();
        if (allocationScope.InvoiceId is not { } invoiceId || invoiceId == Guid.Empty || string.IsNullOrWhiteSpace(allocationScope.SourceCode))
            return mergedEntries;

        var existingJournalEntryIds = mergedEntries.Select(entry => entry.JournalEntryId).ToHashSet();
        var invoiceChargeJournalEntries = (await GetAllJournalEntriesForInvoiceAsync(organizationId, officeId, invoiceId))
            .Where(entry => entry.JournalEntryKindId == JournalEntryKind.Charge
                && EntityCodeFormatting.CodesMatch(entry.SourceCode, allocationScope.SourceCode)
                && MatchesTransferDepositInvoiceChargeJournalEntry(entry, allocationScope));

        foreach (var invoiceChargeJournalEntry in invoiceChargeJournalEntries)
        {
            if (existingJournalEntryIds.Add(invoiceChargeJournalEntry.JournalEntryId))
                mergedEntries.Add(invoiceChargeJournalEntry);
        }

        return mergedEntries;
    }

    private static List<JournalEntry> FilterDepositLinkedJournalEntriesForScope(IReadOnlyList<JournalEntry> depositJournalEntries, TransferDepositAllocationScope allocationScope)
    {
        return depositJournalEntries
            .Where(entry => MatchesTransferDepositAllocationScope(entry, allocationScope))
            .ToList();
    }

    #endregion

    #region Recap Classification

    private static (decimal OwnerEscrow, decimal SecDep, decimal Sdw, string Description) ClassifyTransferDepositAllocation(IReadOnlyList<JournalEntry> scopedDepositJournalEntries, TransferDepositAllocationScope allocationScope, TransferDepositRecapAccountContext recapContext)
    {
        var ownerEscrow = 0m;
        var secDep = 0m;
        var sdw = 0m;
        var hasSecurityDepositActual = scopedDepositJournalEntries.Any(entry => entry.JournalEntryKindId == JournalEntryKind.SecurityDepositActual);
        var hasSecurityDepositWaiverActual = scopedDepositJournalEntries.Any(entry => entry.JournalEntryKindId == JournalEntryKind.SecurityDepositWaiverActual);

        foreach (var entry in scopedDepositJournalEntries)
        {
            AccumulateTransferDepositClassification(
                entry,
                recapContext,
                ref ownerEscrow,
                ref secDep,
                ref sdw,
                includeOwnerEscrow: entry.JournalEntryKindId != JournalEntryKind.Charge,
                preferChargeSecurityDeposit: !hasSecurityDepositActual,
                preferChargeSdw: !hasSecurityDepositWaiverActual);
        }

        return (
            RoundCurrency(ownerEscrow),
            RoundCurrency(secDep),
            RoundCurrency(sdw),
            allocationScope.SourceCode);
    }

    #endregion

    #region Allocation Result

    private static TransferDepositAllocationResult BuildTransferDepositAllocationResult(Guid depositId, Guid? escrowJournalEntryLineId, decimal escrowAmount, TransferDepositAllocationScope allocationScope, decimal ownerEscrow, decimal secDep, decimal sdw, string description)
    {
        var normalizedEscrowAmount = NormalizeTransferEscrowAmount(escrowAmount);
        var fullSplitAmount = RoundCurrency(Math.Abs(allocationScope.SplitAmount));

        if (fullSplitAmount != 0 && Math.Abs(normalizedEscrowAmount - fullSplitAmount) > 0.005m)
        {
            var ratio = normalizedEscrowAmount / fullSplitAmount;
            ownerEscrow = RoundCurrency(ownerEscrow * ratio);
            secDep = RoundCurrency(secDep * ratio);
            sdw = RoundCurrency(sdw * ratio);
        }

        // Business is always the deposit residual (company rent + fees). FeesActual is not the Business split amount.
        var business = RoundCurrency(normalizedEscrowAmount - ownerEscrow - secDep - sdw);

        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
            JournalEntryLineId = escrowJournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = normalizedEscrowAmount,
            OwnerEscrow = ownerEscrow,
            SecDep = secDep,
            Sdw = sdw,
            Business = business,
            PropertyId = allocationScope.PropertyId,
            ReservationId = allocationScope.ReservationId,
            ContactId = allocationScope.ContactId,
            Description = description
        };
    }

    #endregion

    #region Helpers

    private static bool MatchesTransferDepositAllocationScope(JournalEntry entry, TransferDepositAllocationScope scope)
    {
        if (entry.SourceTypeId == (int)SourceType.Deposit)
            return false;

        if (!EntityCodeFormatting.CodesMatch(entry.SourceCode, scope.SourceCode))
            return false;

        if (entry.JournalEntryKindId == JournalEntryKind.Charge
            && entry.SourceTypeId == (int)SourceType.Invoice)
        {
            return MatchesTransferDepositInvoiceChargeJournalEntry(entry, scope);
        }

        return entry.PaymentId == scope.PaymentId;
    }

    private static string ResolveTransferDepositPaymentMemoSourceCode(JournalEntry paymentJournalEntry, JournalEntryLine? paymentLine = null)
    {
        var paymentMemoMatch = MatchPaymentMemo(paymentJournalEntry.Memo, paymentLine?.Memo);
        if (paymentMemoMatch.IsMatch && !string.IsNullOrWhiteSpace(paymentMemoMatch.SourceCode))
            return paymentMemoMatch.SourceCode.Trim();

        return paymentJournalEntry.SourceCode?.Trim() ?? string.Empty;
    }

    private static bool MatchesTransferDepositInvoiceChargeJournalEntry(JournalEntry chargeEntry, TransferDepositAllocationScope scope)
    {
        if (chargeEntry.JournalEntryKindId != JournalEntryKind.Charge
            || chargeEntry.SourceTypeId != (int)SourceType.Invoice)
        {
            return false;
        }

        if (scope.InvoiceId is { } scopeInvoiceId
            && scopeInvoiceId != Guid.Empty
            && chargeEntry.SourceId is { } invoiceId
            && invoiceId != Guid.Empty
            && scopeInvoiceId != invoiceId)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(scope.PaymentMemoSourceCode))
            return true;

        var memo = CoalesceJournalEntryMemo(chargeEntry.Memo, chargeEntry.JournalEntryLines?.FirstOrDefault()?.Memo);
        if (string.IsNullOrWhiteSpace(memo))
            return false;

        var chargePrefix = memo.Split(':', 2, StringSplitOptions.None)[0].Trim();
        return EntityCodeFormatting.CodesMatch(chargePrefix, scope.PaymentMemoSourceCode);
    }

    private static void AccumulateTransferDepositClassification(JournalEntry entry, TransferDepositRecapAccountContext recapContext, ref decimal ownerEscrow, ref decimal secDep, ref decimal sdw, bool includeOwnerEscrow, bool preferChargeSecurityDeposit = true, bool preferChargeSdw = true)
    {
        foreach (var line in entry.JournalEntryLines ?? [])
        {
            if (!JournalEntryRecapLineClassifier.TryClassify(
                    BuildTransferDepositRecapClassificationLine(entry, line, recapContext),
                    out var classification))
            {
                continue;
            }

            switch (classification.RecapCategory)
            {
                case "OwnerRentActual" when includeOwnerEscrow:
                    ownerEscrow = RoundCurrency(ownerEscrow + classification.Amount);
                    break;
                case "SecurityDeposit" when preferChargeSecurityDeposit || entry.JournalEntryKindId != JournalEntryKind.Charge:
                    secDep = RoundCurrency(secDep + classification.Amount);
                    break;
                case "SDW" when preferChargeSdw || entry.JournalEntryKindId != JournalEntryKind.Charge:
                    sdw = RoundCurrency(sdw + classification.Amount);
                    break;
            }
        }
    }

    private TransferDepositRecapAccountContext BuildTransferDepositRecapAccountContext(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        var rentalIncomeAccountIds = chartOfAccounts
            .Where(account => account.OfficeId == officeId && account.AccountType == AccountType.Income)
            .Select(account => account.AccountId)
            .ToHashSet();

        return new TransferDepositRecapAccountContext
        {
            AccountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, officeId, accountingOffice),
            UndepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice),
            PrePaymentAccountId = GetDefaultPrePayment(chartOfAccounts, officeId, accountingOffice),
            OwnerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice),
            OwnerExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice),
            TenantIncomeAccountId = GetDefaultTenantIncome(chartOfAccounts, officeId, accountingOffice),
            EscrowSecDepAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, officeId, accountingOffice),
            EscrowSdwAccountId = GetDefaultEscrowSdwAccount(chartOfAccounts, officeId, accountingOffice),
            EscrowDepositAccountId = GetDefaultEscrowDepositAccount(chartOfAccounts, officeId, accountingOffice),
            RentalIncomeAccountIds = rentalIncomeAccountIds
        };
    }

    private static JournalEntryRecapClassificationLine BuildTransferDepositRecapClassificationLine(JournalEntry entry, JournalEntryLine line, TransferDepositRecapAccountContext recapContext)
    {
        return new JournalEntryRecapClassificationLine
        {
            SourceTypeId = entry.SourceTypeId,
            JournalEntryKindId = (int?)entry.JournalEntryKindId,
            SourceDocumentCode = entry.SourceCode,
            ChartOfAccountId = line.ChartOfAccountId,
            Debit = line.Debit,
            Credit = line.Credit,
            LineMemo = line.Memo,
            JournalEntryMemo = entry.Memo,
            DefaultActRcvableAccountId = recapContext.AccountsReceivableAccountId,
            DefaultUndepFundsAccountId = recapContext.UndepositedFundsAccountId,
            DefaultPrePayAccountId = recapContext.PrePaymentAccountId,
            DefaultOwnActPayableAccountId = recapContext.OwnerAccountsPayableAccountId,
            DefaultOwnerExpAccountId = recapContext.OwnerExpenseAccountId,
            DefaultTenantIncAccountId = recapContext.TenantIncomeAccountId,
            DefaultEscrowSecDepAccountId = recapContext.EscrowSecDepAccountId,
            DefaultEscrowSdwAccountId = recapContext.EscrowSdwAccountId,
            DefaultEscrowDepositAccountId = recapContext.EscrowDepositAccountId,
            IsRentalIncomeAccount = recapContext.RentalIncomeAccountIds.Contains(line.ChartOfAccountId),
            IsCashOnly = entry.IsCashOnly
        };
    }

    private static decimal RoundCurrency(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeTransferEscrowAmount(decimal escrowAmount)
    {
        return RoundCurrency(Math.Abs(escrowAmount));
    }

    private static string ResolveTransferDepositAllocationDescription(string? splitDescription, string? depositCode)
    {
        if (!string.IsNullOrWhiteSpace(splitDescription))
        {
            var trimmed = splitDescription.Trim();
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0)
                return trimmed[..colonIndex].Trim();

            return trimmed;
        }

        return depositCode?.Trim() ?? string.Empty;
    }

    #endregion
}

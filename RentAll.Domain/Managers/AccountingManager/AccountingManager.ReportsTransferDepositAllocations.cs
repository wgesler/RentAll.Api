using RentAll.Domain.Accounting;
using RentAll.Domain.Enums;
using RentAll.Domain.Helpers;
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
        public string PaymentMemo { get; init; } = string.Empty;
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
                        ? RoundTransferEscrowAmount(item.EscrowAmount)
                        : RoundTransferEscrowAmount(singleSplit.Amount),
                    JournalEntryLineId = item.JournalEntryLineId,
                    DepositSplitId = singleSplit.DepositSplitId > 0 ? singleSplit.DepositSplitId : item.DepositSplitId
                });
                continue;
            }

            var requestAmount = RoundTransferEscrowAmount(item.EscrowAmount);
            var depositAmount = RoundCurrency(deposit.Amount);
            var splitTotal = RoundCurrency(deposit.Splits
                .Where(split => Math.Abs(split.Amount) > 0.005m)
                .Sum(split => split.Amount));
            var matchesFullDepositAmount = Math.Abs(requestAmount) > 0.005m
                && (Math.Abs(requestAmount - depositAmount) <= 0.005m
                    || Math.Abs(requestAmount - splitTotal) <= 0.005m);
            if (!matchesFullDepositAmount)
            {
                expandedItems.Add(new TransferDepositAllocationRequestItem
                {
                    DepositId = item.DepositId,
                    EscrowAmount = RoundTransferEscrowAmount(item.EscrowAmount),
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
                    EscrowAmount = RoundTransferEscrowAmount(split.Amount),
                    JournalEntryLineId = item.JournalEntryLineId,
                    DepositSplitId = split.DepositSplitId > 0 ? split.DepositSplitId : null
                });
            }
        }

        return expandedItems;
    }

    #endregion

    #region Resolve Single Deposit Allocation

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationAsync(Guid organizationId, int officeId, Guid depositId, decimal escrowAmount, TransferDepositRecapAccountContext recapContext, Guid? escrowJournalEntryLineId = null, int? depositSplitId = null, bool repairInvalidDepositLinks = true)
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
            depositSplitId,
            repairInvalidDepositLinks);
    }

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationAsync(Guid organizationId, int officeId, Deposit deposit, decimal escrowAmount, TransferDepositRecapAccountContext recapContext, Guid? escrowJournalEntryLineId, int undepositedFundsAccountId, Dictionary<Guid, List<JournalEntry>>? depositJournalEntryCache, int? depositSplitId = null, bool repairInvalidDepositLinks = true)
    {
        escrowAmount = RoundTransferEscrowAmount(escrowAmount);
        var depositId = deposit.DepositId;
        var matchedSplit = RequireTransferDepositSplit(deposit, escrowAmount, depositSplitId);

        if (!IsPaymentBackedDepositSplit(matchedSplit, undepositedFundsAccountId))
            return BuildNonPaymentTransferDepositAllocationResult(depositId, escrowJournalEntryLineId, escrowAmount, deposit, matchedSplit);

        if (repairInvalidDepositLinks && !await IsValidDepositSplitJournalEntryLineAsync(deposit, matchedSplit, undepositedFundsAccountId))
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
        var cardRefundAllocation = await TryBuildSecurityDepositCardRefundTransferAllocationAsync(
            organizationId,
            deposit,
            escrowAmount,
            escrowJournalEntryLineId,
            allocationScope);
        if (cardRefundAllocation != null)
            return cardRefundAllocation;

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

        if (allocationScope.PaymentId == Guid.Empty
            && matchedSplit.JournalEntryLineId is { } returnLineId
            && returnLineId != Guid.Empty)
        {
            depositJournalEntries = depositJournalEntries.ToList();
            var returnLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(returnLineId);
            if (returnLine != null)
            {
                var returnEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(returnLine.JournalEntryId, organizationId);
                if (returnEntry != null
                    && IsSecurityDepositReturnJournalEntry(returnEntry)
                    && depositJournalEntries.All(entry => entry.JournalEntryId != returnEntry.JournalEntryId))
                {
                    depositJournalEntries.Add(returnEntry);
                }
            }
        }

        depositJournalEntries = await MergeTransferDepositPaymentJournalEntriesAsync(
            organizationId,
            depositJournalEntries,
            allocationScope);
        depositJournalEntries = await MergeTransferDepositInvoiceChargeJournalEntriesAsync(organizationId, deposit.OfficeId, depositJournalEntries, allocationScope);

        var scopedDepositJournalEntries = FilterDepositLinkedJournalEntriesForScope(depositJournalEntries, allocationScope);
        if (scopedDepositJournalEntries.Count == 0)
        {
            throw new InvalidOperationException(
                $"No deposit-linked journal entries matched payment {allocationScope.SourceCode} for deposit {deposit.DepositCode}.");
        }

        var (ownerEscrow, secDep, sdw) =
            ClassifyTransferDepositAllocation(scopedDepositJournalEntries, recapContext);
        AlignTransferDepositClassificationWithSplitSign(allocationScope.SplitAmount, ref ownerEscrow, ref secDep, ref sdw);
        var description = await ResolveTransferDepositAllocationDisplaySourceAsync(
            organizationId,
            allocationScope,
            matchedSplit,
            scopedDepositJournalEntries);

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
        var roundedEscrowAmount = RoundTransferEscrowAmount(escrowAmount);
        var description = ResolveTransferDepositAllocationDescription(split.Description, deposit.DepositCode);

        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
            JournalEntryLineId = escrowJournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = roundedEscrowAmount,
            OwnerEscrow = 0m,
            SecDep = 0m,
            Sdw = 0m,
            Business = roundedEscrowAmount,
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

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, transfer.OfficeId);
        var recapContext = BuildTransferDepositRecapAccountContext(chartOfAccounts, transfer.OfficeId, accountingOffice);
        var ownersAccountId = accountingOffice?.DefaultEscrowOwnersAccountId;
        var secDepAccountId = accountingOffice?.DefaultEscrowSecDepAccountId;
        var sdwAccountId = accountingOffice?.DefaultEscrowSdwAccountId;
        var businessAccountId = accountingOffice?.DefaultBankAccountId;
        var escrowDepositAccountId = accountingOffice?.DefaultEscrowDepositAccountId;
        var transferBankAccountId = transfer.BankAccountId is > 0
            ? transfer.BankAccountId
            : escrowDepositAccountId;

        var results = new List<TransferReportLineAllocationResult>();
        var reportSplits = (transfer.Splits ?? [])
            .Where(split => Math.Abs(split.Amount) > 0.005m)
            .ToList();
        var securityDepositSplits = reportSplits
            .Where(split => IsSecurityDepositAllocationMemo(split.Description))
            .ToList();
        if (securityDepositSplits.Count > 0)
        {
            results.Add(BuildSecurityDepositTransferReportRow(
                securityDepositSplits,
                transferBankAccountId,
                escrowDepositAccountId,
                ownersAccountId,
                secDepAccountId,
                sdwAccountId,
                businessAccountId));
        }

        var linkedGroups = reportSplits
            .Where(split => !IsSecurityDepositAllocationMemo(split.Description)
                && split.JournalEntryLineId is { } lineId && lineId != Guid.Empty)
            .GroupBy(split => BuildTransferReportSplitGroupKey(split));

        foreach (var group in linkedGroups)
        {
            var splits = group.ToList();
            var classified = await TryClassifyTransferReportGroupAsync(organizationId, transfer.OfficeId, splits, recapContext);
            results.Add(classified ?? BuildTransferReportLineAllocationFromSplits(
                splits,
                splits[0].JournalEntryLineId ?? Guid.Empty,
                ownersAccountId,
                secDepAccountId,
                sdwAccountId,
                businessAccountId));
        }

        foreach (var manualGroup in GroupUnlinkedTransferSplitsForReport(reportSplits.Where(split => !IsSecurityDepositAllocationMemo(split.Description)).ToList()))
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

    private static string BuildTransferReportSplitGroupKey(TransferSplit split)
    {
        var lineId = split.JournalEntryLineId ?? Guid.Empty;
        var description = (split.Description ?? string.Empty).Trim();
        if (lineId != Guid.Empty && description.Length > 0)
            return $"{lineId:N}|{description}";

        if (lineId != Guid.Empty)
            return lineId.ToString("N");

        if (description.Length > 0)
            return description;

        return $"ctx:{split.PropertyId}:{split.ReservationId}:{split.ContactId}";
    }

    private static bool IsSecurityDepositCardRefundPayment(Payment payment)
        => payment.PaymentTypeId is int paymentTypeId
            && IsSecurityDepositCardRefund((PaymentType)paymentTypeId)
            && IsSecurityDepositRefundMemo(payment.Description);

    private static bool IsSecurityDepositAllocation(IReadOnlyList<TransferSplit> splits)
        => splits.Count > 0 && splits.All(split => IsSecurityDepositAllocationMemo(split.Description));

    private static TransferReportLineAllocationResult BuildSecurityDepositTransferReportRow(
        IReadOnlyList<TransferSplit> splits,
        int? transferBankAccountId,
        int? escrowDepositAccountId,
        int? ownersAccountId,
        int? secDepAccountId,
        int? sdwAccountId,
        int? businessAccountId)
    {
        var escrow = 0m;
        var ownerEscrow = 0m;
        var secDep = 0m;
        var sdw = 0m;
        var business = 0m;
        var contextSplit = splits[0];

        foreach (var split in splits)
        {
            if (split.ChartOfAccountId is not > 0 || split.Amount == 0m)
                continue;

            var amount = RoundCurrency(split.Amount);
            var accountId = split.ChartOfAccountId.Value;
            if (escrowDepositAccountId is > 0 && accountId == escrowDepositAccountId)
                escrow += amount > 0m ? -amount : amount;
            else if (businessAccountId is > 0 && accountId == businessAccountId)
                business += amount;
            else if (ownersAccountId is > 0 && accountId == ownersAccountId)
                ownerEscrow += amount;
            else if (secDepAccountId is > 0 && accountId == secDepAccountId)
                secDep += amount;
            else if (sdwAccountId is > 0 && accountId == sdwAccountId)
                sdw += amount;

            if (transferBankAccountId is not > 0 || transferBankAccountId == accountId)
                continue;

            var credit = RoundCurrency(-amount);
            if (transferBankAccountId == escrowDepositAccountId)
                escrow += credit < 0m ? Math.Abs(credit) : credit;
            else if (transferBankAccountId == businessAccountId)
                business += credit;
            else if (transferBankAccountId == ownersAccountId)
                ownerEscrow += credit;
            else if (transferBankAccountId == secDepAccountId)
                secDep += credit;
            else if (transferBankAccountId == sdwAccountId)
                sdw += credit;
        }

        return new TransferReportLineAllocationResult
        {
            JournalEntryLineId = contextSplit.JournalEntryLineId ?? Guid.Empty,
            DepositId = Guid.Empty,
            EscrowAmount = RoundCurrency(escrow),
            OwnerEscrow = RoundCurrency(ownerEscrow),
            SecDep = RoundCurrency(secDep),
            Sdw = RoundCurrency(sdw),
            Business = RoundCurrency(business),
            PropertyId = contextSplit.PropertyId,
            ReservationId = contextSplit.ReservationId,
            ContactId = contextSplit.ContactId,
            Description = splits
                .Select(split => split.Description)
                .FirstOrDefault(value => IsSecurityDepositAllocationMemo(value) && !IsSecurityDepositAllocationTbrMemo(value))
                ?? contextSplit.Description
                ?? string.Empty
        };
    }

    private static TransferReportLineAllocationResult BuildSecurityDepositAllocationRow(IReadOnlyList<TransferSplit> splits, int? businessAccountId)
    {
        var contextSplit = splits[0];
        var markedTbr = splits.Where(split => IsSecurityDepositAllocationTbrMemo(split.Description)).Sum(split => split.Amount);
        var business = splits.Any(split => IsSecurityDepositAllocationTbrMemo(split.Description))
            ? splits.Where(split => !IsSecurityDepositAllocationTbrMemo(split.Description)).Sum(split => split.Amount)
            : businessAccountId is > 0
                ? splits.Where(split => split.ChartOfAccountId == businessAccountId).Sum(split => split.Amount)
                : 0m;
        var tbr = splits.Any(split => IsSecurityDepositAllocationTbrMemo(split.Description))
            ? markedTbr
            : splits.Where(split => businessAccountId is not > 0 || split.ChartOfAccountId != businessAccountId).Sum(split => split.Amount);
        return new TransferReportLineAllocationResult
        {
            JournalEntryLineId = Guid.Empty,
            DepositId = Guid.Empty,
            EscrowAmount = RoundCurrency(business + tbr),
            OwnerEscrow = 0m,
            SecDep = RoundCurrency(tbr),
            Sdw = 0m,
            Business = RoundCurrency(business),
            PropertyId = contextSplit.PropertyId,
            ReservationId = contextSplit.ReservationId,
            ContactId = contextSplit.ContactId,
            Description = splits
                .Select(split => split.Description)
                .FirstOrDefault(value => IsSecurityDepositAllocationMemo(value) && !IsSecurityDepositAllocationTbrMemo(value))
                ?? contextSplit.Description
                ?? string.Empty
        };
    }

    private static TransferReportLineAllocationResult BuildTransferReportLineAllocationFromSplits(IReadOnlyList<TransferSplit> splits, Guid journalEntryLineId, int? ownersAccountId, int? secDepAccountId, int? sdwAccountId, int? businessAccountId)
    {
        var contextSplit = splits[0];
        var description = splits
            .Select(split => (split.Description ?? string.Empty).Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? string.Empty;
        var (ownerEscrow, secDep, sdw, business) = SumTransferReportSplitsOnce(splits, ownersAccountId, secDepAccountId, sdwAccountId, businessAccountId);
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
            return NormalizeTransferReportEscrowSourceAmount(sourceAmount.Value, destinationTotal);

        return destinationTotal;
    }

    private static decimal NormalizeTransferReportEscrowSourceAmount(decimal sourceAmount, decimal destinationTotal)
    {
        var source = RoundCurrency(sourceAmount);
        var destination = RoundCurrency(destinationTotal);
        if (Math.Abs(source) <= 0.005m)
            return destination;

        // Normal outflow from escrow: credited on 1002 (negative net), positive destination splits.
        if (source < 0 && destination > 0.005m)
            return RoundCurrency(Math.Abs(source));

        return source;
    }

    private async Task<TransferReportLineAllocationResult?> TryClassifyTransferReportGroupAsync(Guid organizationId, int officeId, IReadOnlyList<TransferSplit> splits, TransferDepositRecapAccountContext recapContext)
    {
        var journalEntryLineId = splits.Select(split => split.JournalEntryLineId).FirstOrDefault(lineId => lineId is { } value && value != Guid.Empty);
        if (journalEntryLineId is not { } sourceLineId)
            return null;

        var sourceLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(sourceLineId);
        if (sourceLine == null)
            return null;

        var sourceEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(sourceLine.JournalEntryId, organizationId);
        var depositId = sourceEntry?.DepositId ?? Guid.Empty;
        if (depositId == Guid.Empty && sourceEntry?.SourceTypeId == (int)SourceType.Deposit && sourceEntry.SourceId is { } sourceId)
            depositId = sourceId;
        if (depositId == Guid.Empty)
            return null;

        var escrowAmount = RoundCurrency(splits.Sum(split => split.Amount));
        if (Math.Abs(escrowAmount) <= 0.005m)
            return null;

        try
        {
            var allocation = await ResolveTransferDepositAllocationAsync(organizationId, officeId, depositId, escrowAmount, recapContext, sourceLineId, repairInvalidDepositLinks: false);
            var context = splits[0];
            var description = splits.Select(split => (split.Description ?? string.Empty).Trim()).FirstOrDefault(value => value.Length > 0) ?? allocation.Description;
            return new TransferReportLineAllocationResult
            {
                JournalEntryLineId = sourceLineId,
                DepositId = allocation.DepositId,
                EscrowAmount = allocation.EscrowAmount,
                OwnerEscrow = allocation.OwnerEscrow,
                SecDep = allocation.SecDep,
                Sdw = allocation.Sdw,
                Business = allocation.Business,
                PropertyId = context.PropertyId ?? allocation.PropertyId,
                ReservationId = context.ReservationId ?? allocation.ReservationId,
                ContactId = context.ContactId ?? allocation.ContactId,
                Description = description
            };
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static (decimal OwnerEscrow, decimal SecDep, decimal Sdw, decimal Business) SumTransferReportSplitsOnce(IReadOnlyList<TransferSplit> splits, int? ownersAccountId, int? secDepAccountId, int? sdwAccountId, int? businessAccountId)
    {
        var ownerEscrow = 0m;
        var secDep = 0m;
        var sdw = 0m;
        var business = 0m;
        foreach (var split in splits)
        {
            if (split.ChartOfAccountId is not > 0 || split.Amount == 0m)
                continue;

            var accountId = split.ChartOfAccountId.Value;
            var isOwner = ownersAccountId is > 0 && accountId == ownersAccountId;
            var isSecurityDeposit = secDepAccountId is > 0 && accountId == secDepAccountId;
            var isSdw = sdwAccountId is > 0 && accountId == sdwAccountId;
            var isBusiness = businessAccountId is > 0 && accountId == businessAccountId;
            var matchCount = (isOwner ? 1 : 0) + (isSecurityDeposit ? 1 : 0) + (isSdw ? 1 : 0) + (isBusiness ? 1 : 0);
            if (matchCount != 1)
            {
                if (isBusiness)
                    business += split.Amount;
                continue;
            }

            if (isOwner)
                ownerEscrow += split.Amount;
            else if (isSecurityDeposit)
                secDep += split.Amount;
            else if (isSdw)
                sdw += split.Amount;
            else
                business += split.Amount;
        }

        return (RoundCurrency(ownerEscrow), RoundCurrency(secDep), RoundCurrency(sdw), RoundCurrency(business));
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
        var roundedEscrowAmount = RoundTransferEscrowAmount(escrowAmount);
        if (Math.Abs(roundedEscrowAmount) > 0.005m)
        {
            var amountMatches = splits
                .Where(split => Math.Abs(RoundCurrency(split.Amount - roundedEscrowAmount)) <= 0.005m)
                .ToList();

            if (amountMatches.Count == 1)
                return amountMatches[0];

            if (amountMatches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Deposit {deposit.DepositCode} has multiple splits matching escrow amount {roundedEscrowAmount:0.00}.");
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
            if (!IsSecurityDepositReturnJournalEntry(paymentJournalEntry))
            {
                throw new InvalidOperationException(
                    $"Payment journal entry {paymentJournalEntry.JournalEntryCode} is missing PaymentId for deposit {deposit.DepositCode}.");
            }

            var returnSourceCode = paymentJournalEntry.SourceCode?.Trim() ?? string.Empty;
            return new TransferDepositAllocationScope
            {
                PaymentId = Guid.Empty,
                SourceCode = returnSourceCode,
                PaymentMemoSourceCode = returnSourceCode,
                PaymentMemo = CoalesceJournalEntryMemo(paymentJournalEntry.Memo, paymentLine.Memo),
                SplitAmount = split.Amount,
                PropertyId = split.PropertyId,
                ReservationId = split.ReservationId,
                ContactId = split.ContactId
            };
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
            PaymentMemo = CoalesceJournalEntryMemo(paymentJournalEntry.Memo, paymentLine?.Memo),
            SplitAmount = split.Amount,
            PropertyId = split.PropertyId,
            ReservationId = split.ReservationId,
            ContactId = split.ContactId
        };
    }

    #endregion

    #region Deposit-Linked Journal Entry Selection

    private async Task<List<JournalEntry>> MergeTransferDepositPaymentJournalEntriesAsync(Guid organizationId, IReadOnlyList<JournalEntry> depositJournalEntries, TransferDepositAllocationScope allocationScope)
    {
        var mergedEntries = depositJournalEntries.ToList();
        if (allocationScope.PaymentId == Guid.Empty)
            return mergedEntries;

        var existingJournalEntryIds = mergedEntries.Select(entry => entry.JournalEntryId).ToHashSet();
        var paymentJournalEntries = await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria
        {
            OrganizationId = organizationId,
            PaymentId = allocationScope.PaymentId
        });

        foreach (var paymentJournalEntry in paymentJournalEntries)
        {
            if (paymentJournalEntry.SourceTypeId == (int)SourceType.Deposit)
                continue;

            if (existingJournalEntryIds.Add(paymentJournalEntry.JournalEntryId))
                mergedEntries.Add(paymentJournalEntry);
        }

        return mergedEntries;
    }

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

    private static (decimal OwnerEscrow, decimal SecDep, decimal Sdw) ClassifyTransferDepositAllocation(IReadOnlyList<JournalEntry> scopedDepositJournalEntries, TransferDepositRecapAccountContext recapContext)
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
            RoundCurrency(sdw));
    }

    #endregion

    private async Task<TransferDepositAllocationResult?> TryBuildSecurityDepositCardRefundTransferAllocationAsync(
        Guid organizationId,
        Deposit deposit,
        decimal escrowAmount,
        Guid? escrowJournalEntryLineId,
        TransferDepositAllocationScope allocationScope)
    {
        if (allocationScope.PaymentId == Guid.Empty)
            return null;

        var payment = await _accountingRepository.GetPaymentByIdAsync(allocationScope.PaymentId, organizationId);
        if (payment == null)
            return null;

        var reservationId = allocationScope.ReservationId is { } scopeReservationId && scopeReservationId != Guid.Empty
            ? scopeReservationId
            : payment.LedgerLines
                .Select(line => line.ReservationId)
                .FirstOrDefault(id => id is { } value && value != Guid.Empty);
        if (reservationId is not { } resolvedReservationId || resolvedReservationId == Guid.Empty)
            return null;

        var invoices = await GetInvoicesForReservationsAsync(organizationId, deposit.OfficeId, [resolvedReservationId]);
        var reservationCode = invoices
            .Select(invoice => invoice.ReservationCode)
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code))
            ?? ExtractReservationCode(payment.Description);
        var applicationPayments = await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices, reservationCode);
        Payment? cardRefund = null;
        if (IsSecurityDepositCardRefundPayment(payment))
            cardRefund = payment;
        else if (IsSecurityDepositPaymentMemo(payment.Description, reservationCode))
            cardRefund = applicationPayments.FirstOrDefault(IsSecurityDepositCardRefundPayment);
        if (cardRefund == null)
            return null;

        payment = cardRefund;
        var fullAmount = RoundSecurityDepositAmount(applicationPayments
            .Where(item => IsSecurityDepositPaymentMemo(item.Description, reservationCode))
            .Sum(item => item.Amount));
        var refundAmount = RoundSecurityDepositAmount(Math.Abs(payment.Amount));
        if (fullAmount <= 0.005m)
            fullAmount = refundAmount;
        var chargeAmount = RoundSecurityDepositAmount(Math.Max(0m, fullAmount - refundAmount));
        if (chargeAmount + refundAmount <= 0.005m)
            return null;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, deposit.OfficeId);
        var sourceBankAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, deposit.OfficeId, accountingOffice);
        var businessAccountId = GetDefaultBankAccount(chartOfAccounts, deposit.OfficeId, accountingOffice);
        var refundBankAccountId = deposit.BankAccountId is > 0
            ? deposit.BankAccountId.Value
            : GetDefaultEscrowDepositAccount(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (sourceBankAccountId <= 0)
            throw new InvalidOperationException("Security deposit escrow is not configured for this office");
        if (chargeAmount > 0.005m && businessAccountId <= 0)
            throw new InvalidOperationException("Business bank is not configured for this office");
        if (refundAmount > 0.005m && refundBankAccountId <= 0)
            throw new InvalidOperationException("Escrow deposit account is not configured for this office");

        var invoiceCode = payment.LedgerLines
            .Select(line => (line.InvoiceCode ?? string.Empty).Trim())
            .FirstOrDefault(code => code.Length > 0)
            ?? reservationCode;
        var description = BuildSecurityDepositTransferCreditMemo(invoiceCode);

        return new TransferDepositAllocationResult
        {
            DepositId = deposit.DepositId,
            JournalEntryLineId = escrowJournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = RoundTransferEscrowAmount(escrowAmount),
            SecurityDepositCardRefund = true,
            SourceBankAccountId = sourceBankAccountId,
            ChargeAmount = chargeAmount,
            RefundAmount = refundAmount,
            RefundBankAccountId = refundBankAccountId,
            RefundDescription = $"{description} TBR",
            PropertyId = allocationScope.PropertyId,
            ReservationId = resolvedReservationId,
            ContactId = allocationScope.ContactId,
            Description = description
        };
    }

    #region Allocation Result

    private static TransferDepositAllocationResult BuildTransferDepositAllocationResult(Guid depositId, Guid? escrowJournalEntryLineId, decimal escrowAmount, TransferDepositAllocationScope allocationScope, decimal ownerEscrow, decimal secDep, decimal sdw, string description)
    {
        var roundedEscrowAmount = RoundTransferEscrowAmount(escrowAmount);
        var fullSplitAmount = RoundCurrency(allocationScope.SplitAmount);

        if (Math.Abs(fullSplitAmount) > 0.005m && Math.Abs(roundedEscrowAmount - fullSplitAmount) > 0.005m)
        {
            var ratio = roundedEscrowAmount / fullSplitAmount;
            ownerEscrow = RoundCurrency(ownerEscrow * ratio);
            secDep = RoundCurrency(secDep * ratio);
            sdw = RoundCurrency(sdw * ratio);
        }

        // Business is always the deposit residual (company rent + fees). FeesActual is not the Business split amount.
        var business = RoundCurrency(roundedEscrowAmount - ownerEscrow - secDep - sdw);

        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
            JournalEntryLineId = escrowJournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = roundedEscrowAmount,
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

        if (scope.PaymentId == Guid.Empty && IsSecurityDepositReturnJournalEntry(entry))
            return true;

        // Owner/SD/SDW actuals keep invoice/reservation source codes — link by PaymentId, not PY-xxx SourceCode.
        if (entry.PaymentId == scope.PaymentId && scope.PaymentId != Guid.Empty)
            return true;

        if (!EntityCodeFormatting.CodesMatch(entry.SourceCode, scope.SourceCode))
            return false;

        if (entry.JournalEntryKindId == JournalEntryKind.Charge
            && entry.SourceTypeId == (int)SourceType.Invoice)
        {
            return MatchesTransferDepositInvoiceChargeJournalEntry(entry, scope);
        }

        return EntityCodeFormatting.CodesMatch(entry.SourceCode, scope.SourceCode);
    }

    private static string ResolveTransferDepositPaymentMemoSourceCode(JournalEntry paymentJournalEntry, JournalEntryLine? paymentLine = null)
    {
        var paymentMemoMatch = MatchPaymentMemo(paymentJournalEntry.Memo, paymentLine?.Memo);
        if (paymentMemoMatch.IsMatch && paymentMemoMatch.SourceCodes.Count == 1)
            return paymentMemoMatch.SourceCodes[0].Trim();

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
        if (!string.IsNullOrWhiteSpace(scope.PaymentMemo))
            return PaymentMemoMatchesInvoiceSourceCode(scope.PaymentMemo, chargePrefix);

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

    private static decimal RoundTransferEscrowAmount(decimal escrowAmount)
        => RoundCurrency(escrowAmount);

    private static void AlignTransferDepositClassificationWithSplitSign(decimal splitAmount, ref decimal ownerEscrow, ref decimal secDep, ref decimal sdw)
    {
        if (Math.Abs(splitAmount) <= 0.005m)
            return;

        var splitSign = Math.Sign(splitAmount);
        if (Math.Abs(ownerEscrow) > 0.005m && Math.Sign(ownerEscrow) != splitSign)
            ownerEscrow = RoundCurrency(-ownerEscrow);
        if (Math.Abs(secDep) > 0.005m && Math.Sign(secDep) != splitSign)
            secDep = RoundCurrency(-secDep);
        if (Math.Abs(sdw) > 0.005m && Math.Sign(sdw) != splitSign)
            sdw = RoundCurrency(-sdw);
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

    private async Task<string> ResolveTransferDepositAllocationDisplaySourceAsync(Guid organizationId, TransferDepositAllocationScope scope, DepositSplit split, IReadOnlyList<JournalEntry> scopedDepositJournalEntries)
    {
        var fromSplit = ResolveDepositSplitInvoiceSourceCode(split);
        if (IsReservationOrInvoiceSourceCode(fromSplit))
            return fromSplit!;

        if (IsReservationOrInvoiceSourceCode(scope.PaymentMemoSourceCode))
            return scope.PaymentMemoSourceCode.Trim();

        var fromScoped = ResolveDominantInvoiceSourceCodeFromScopedJournalEntries(scopedDepositJournalEntries);
        if (!string.IsNullOrWhiteSpace(fromScoped))
            return fromScoped;

        if (scope.SourceCode.StartsWith("PY-", StringComparison.OrdinalIgnoreCase))
        {
            var fromPayment = await ResolveDominantPaymentInvoiceCodeAsync(scope.PaymentId, organizationId);
            if (!string.IsNullOrWhiteSpace(fromPayment))
                return fromPayment;

            var reservationCode = await ResolveReservationCodeForTransferDisplayAsync(scope.ReservationId, organizationId)
                ?? await ResolveReservationCodeForTransferDisplayAsync(split.ReservationId, organizationId);
            if (!string.IsNullOrWhiteSpace(reservationCode))
                return reservationCode;
        }

        if (IsReservationOrInvoiceSourceCode(scope.SourceCode))
            return scope.SourceCode.Trim();

        return scope.SourceCode.Trim();
    }

    internal async Task<string?> ResolvePaymentBackedDepositSplitInvoiceSourceCodeAsync(Guid organizationId, DepositSplit split)
    {
        if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
            return null;

        var paymentLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(lineId);
        if (paymentLine == null)
            return null;

        var paymentJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(paymentLine.JournalEntryId, organizationId);
        if (paymentJournalEntry?.PaymentId is not { } paymentId || paymentId == Guid.Empty)
            return null;

        return await ResolveDominantPaymentInvoiceCodeAsync(paymentId, organizationId);
    }

    private async Task<string?> ResolveDominantPaymentInvoiceCodeAsync(Guid paymentId, Guid organizationId)
    {
        if (paymentId == Guid.Empty)
            return null;

        var ledgerLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(paymentId, organizationId);
        if (ledgerLines == null || ledgerLines.Count == 0)
            return null;

        return ledgerLines
            .Where(line => IsReservationOrInvoiceSourceCode(line.InvoiceCode))
            .OrderByDescending(line => Math.Abs(line.Amount))
            .Select(line => line.InvoiceCode.Trim())
            .FirstOrDefault();
    }

    private async Task<string?> ResolveReservationCodeForTransferDisplayAsync(Guid? reservationId, Guid organizationId)
    {
        if (reservationId is not { } id || id == Guid.Empty)
            return null;

        var reservation = await _reservationRepository.GetReservationByIdAsync(id, organizationId);
        return string.IsNullOrWhiteSpace(reservation?.ReservationCode)
            ? null
            : reservation.ReservationCode.Trim();
    }

    private static string? ResolveDominantInvoiceSourceCodeFromScopedJournalEntries(IReadOnlyList<JournalEntry> scopedDepositJournalEntries)
    {
        return scopedDepositJournalEntries
            .Where(entry => IsReservationOrInvoiceSourceCode(entry.SourceCode))
            .GroupBy(entry => entry.SourceCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Sum(entry =>
                (entry.JournalEntryLines ?? []).Sum(line => Math.Abs(line.Debit - line.Credit))))
            .Select(group => group.Key)
            .FirstOrDefault();
    }

    private static bool IsReservationOrInvoiceSourceCode(string? sourceCode)
        => !string.IsNullOrWhiteSpace(sourceCode)
            && sourceCode.Trim().StartsWith("R-", StringComparison.OrdinalIgnoreCase);

    #endregion
}

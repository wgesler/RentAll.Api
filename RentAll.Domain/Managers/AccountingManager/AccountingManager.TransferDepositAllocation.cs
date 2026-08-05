using RentAll.Domain.Accounting;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private class TransferDepositRecapAccountContext
    {
        public int AccountsReceivableAccountId { get; init; }
        public int UndepositedFundsAccountId { get; init; }
        public int PrePaymentAccountId { get; init; }
        public int OwnerAccountsPayableAccountId { get; init; }
        public int OwnerExpenseAccountId { get; init; }
        public int TenantIncomeAccountId { get; init; }
        public HashSet<int> RentalIncomeAccountIds { get; init; } = [];
    }

    private class TransferDepositAllocationScope
    {
        public Guid? PaymentId { get; set; }
        public Guid? InvoiceId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? ContactId { get; set; }
        public string? SourceCode { get; set; }
        public decimal? SplitAmount { get; set; }
    }

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
                continue;

            results.Add(await ResolveTransferDepositAllocationAsync(
                organizationId,
                officeId,
                item.DepositId,
                item.EscrowAmount,
                recapContext,
                item.JournalEntryLineId));
        }

        return results;
    }

    private async Task<IReadOnlyList<TransferDepositAllocationRequestItem>> ExpandTransferDepositAllocationItemsAsync(
        Guid organizationId,
        IReadOnlyList<TransferDepositAllocationRequestItem> items)
    {
        var expandedItems = new List<TransferDepositAllocationRequestItem>();

        foreach (var item in items)
        {
            if (item.DepositId == Guid.Empty)
                continue;

            var deposit = await _accountingRepository.GetDepositByIdAsync(item.DepositId, organizationId);
            if (deposit?.Splits == null || deposit.Splits.Count == 0)
            {
                expandedItems.Add(item);
                continue;
            }

            if (deposit.Splits.Count == 1)
            {
                var singleSplit = deposit.Splits[0];
                expandedItems.Add(new TransferDepositAllocationRequestItem
                {
                    DepositId = item.DepositId,
                    EscrowAmount = Math.Abs(RoundCurrency(item.EscrowAmount)) > 0.005m
                        ? item.EscrowAmount
                        : singleSplit.Amount,
                    JournalEntryLineId = singleSplit.JournalEntryLineId is { } singleSplitLineId && singleSplitLineId != Guid.Empty
                        ? singleSplitLineId
                        : item.JournalEntryLineId
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
                expandedItems.Add(item);
                continue;
            }

            foreach (var split in deposit.Splits.Where(split => Math.Abs(split.Amount) > 0.005m))
            {
                expandedItems.Add(new TransferDepositAllocationRequestItem
                {
                    DepositId = item.DepositId,
                    EscrowAmount = split.Amount,
                    JournalEntryLineId = split.JournalEntryLineId is { } splitLineId && splitLineId != Guid.Empty
                        ? splitLineId
                        : item.JournalEntryLineId
                });
            }
        }

        return expandedItems;
    }

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationAsync(Guid organizationId, int officeId, Guid depositId, decimal escrowAmount, TransferDepositRecapAccountContext recapContext, Guid? journalEntryLineId = null)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
            return BuildFallbackTransferDepositAllocationResult(depositId, journalEntryLineId, escrowAmount);

        var matchedSplit = FindTransferDepositSplit(deposit, escrowAmount, journalEntryLineId);
        var resolvedJournalEntryLineId = journalEntryLineId;
        TransferDepositAllocationScope allocationScope;

        if (matchedSplit != null)
        {
            allocationScope = await BuildTransferDepositAllocationScopeFromDepositSplitAsync(organizationId, deposit, matchedSplit);
            if (matchedSplit.JournalEntryLineId is { } splitLineId && splitLineId != Guid.Empty)
                resolvedJournalEntryLineId = splitLineId;
            else
            {
                var resolvedPaymentLineId = await ResolveDepositSplitPaymentLineIdAsync(organizationId, deposit, matchedSplit);
                if (resolvedPaymentLineId is { } paymentLineId && paymentLineId != Guid.Empty)
                    resolvedJournalEntryLineId = paymentLineId;
            }
        }
        else
        {
            allocationScope = BuildTransferDepositAllocationScopeFromDepositHeader(deposit, RoundCurrency(Math.Abs(escrowAmount)))
                ?? new TransferDepositAllocationScope
                {
                    PropertyId = deposit.PropertyId ?? deposit.PropertyIds.FirstOrDefault(),
                    SourceCode = deposit.DepositCode,
                    SplitAmount = RoundCurrency(Math.Abs(escrowAmount))
                };
        }

        var depositJournalEntries = (await _journalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria
        {
            OrganizationId = organizationId,
            DepositId = depositId
        })).ToList();

        var scopedDepositJournalEntries = await LoadScopedTransferDepositJournalEntriesAsync(organizationId, depositId, depositJournalEntries, allocationScope);
        var (ownerEscrow, secDep, sdw, fee, propertyId, reservationId, contactId, description) =
            await ClassifyTransferDepositAllocationAsync(organizationId, officeId, scopedDepositJournalEntries, allocationScope, recapContext);

        return BuildTransferDepositAllocationResult(
            depositId,
            resolvedJournalEntryLineId,
            escrowAmount,
            deposit,
            allocationScope,
            ownerEscrow,
            secDep,
            sdw,
            fee,
            propertyId,
            reservationId,
            contactId,
            description);
    }

    private static DepositSplit? FindTransferDepositSplit(Deposit deposit, decimal escrowAmount, Guid? journalEntryLineId)
    {
        var splits = (deposit.Splits ?? [])
            .Where(split => Math.Abs(split.Amount) > 0.005m)
            .ToList();
        if (splits.Count == 0)
            return null;

        if (journalEntryLineId is { } lineId && lineId != Guid.Empty)
        {
            var splitByLine = splits.FirstOrDefault(split => split.JournalEntryLineId == lineId);
            if (splitByLine != null)
                return splitByLine;
        }

        var normalizedEscrowAmount = RoundCurrency(Math.Abs(escrowAmount));
        if (normalizedEscrowAmount != 0)
        {
            var splitByAmount = splits.FirstOrDefault(split =>
                Math.Abs(Math.Abs(split.Amount) - normalizedEscrowAmount) <= 0.005m);
            if (splitByAmount != null)
                return splitByAmount;
        }

        return splits.Count == 1 ? splits[0] : null;
    }

    private async Task<TransferDepositAllocationScope> BuildTransferDepositAllocationScopeFromDepositSplitAsync(Guid organizationId, Deposit deposit, DepositSplit split)
    {
        if (split.JournalEntryLineId is { } existingLineId && existingLineId != Guid.Empty)
        {
            var existingScope = await BuildTransferDepositAllocationScopeFromSplitAsync(organizationId, split, existingLineId);
            if (existingScope != null)
                return existingScope;
        }

        var paymentLineId = await ResolveDepositSplitPaymentLineIdAsync(organizationId, deposit, split);
        if (paymentLineId is { } resolvedPaymentLineId && resolvedPaymentLineId != Guid.Empty)
        {
            var resolvedScope = await BuildTransferDepositAllocationScopeFromSplitAsync(organizationId, split, resolvedPaymentLineId);
            if (resolvedScope != null)
                return resolvedScope;
        }

        return BuildTransferDepositAllocationScopeFromDepositSplit(split);
    }

    private static TransferDepositAllocationResult BuildFallbackTransferDepositAllocationResult(Guid depositId, Guid? journalEntryLineId, decimal escrowAmount)
    {
        var normalizedEscrowAmount = RoundCurrency(Math.Abs(escrowAmount));
        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
            JournalEntryLineId = journalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
            EscrowAmount = normalizedEscrowAmount,
            Business = normalizedEscrowAmount,
            Description = string.Empty
        };
    }

    private static bool MatchesFullTransferDepositAmount(Deposit deposit, decimal normalizedEscrowAmount)
    {
        if (normalizedEscrowAmount == 0)
            return false;

        var depositAmount = RoundCurrency(Math.Abs(deposit.Amount));
        if (Math.Abs(normalizedEscrowAmount - depositAmount) <= 0.005m)
            return true;

        if (deposit.Splits == null || deposit.Splits.Count == 0)
            return false;

        var splitTotal = RoundCurrency(deposit.Splits
            .Where(split => Math.Abs(split.Amount) > 0.005m)
            .Sum(split => Math.Abs(split.Amount)));
        return splitTotal > 0 && Math.Abs(normalizedEscrowAmount - splitTotal) <= 0.005m;
    }

    private static TransferDepositAllocationScope? BuildTransferDepositAllocationScopeFromDepositHeader(Deposit deposit, decimal normalizedEscrowAmount)
    {
        if (normalizedEscrowAmount == 0 && RoundCurrency(Math.Abs(deposit.Amount)) == 0)
            return null;

        return new TransferDepositAllocationScope
        {
            PropertyId = deposit.PropertyId ?? deposit.PropertyIds.FirstOrDefault(),
            SourceCode = deposit.DepositCode,
            SplitAmount = normalizedEscrowAmount != 0 ? normalizedEscrowAmount : RoundCurrency(Math.Abs(deposit.Amount))
        };
    }

    private static TransferDepositAllocationScope BuildTransferDepositAllocationScopeFromDepositSplit(DepositSplit split)
    {
        return new TransferDepositAllocationScope
        {
            PropertyId = split.PropertyId,
            ReservationId = split.ReservationId,
            ContactId = split.ContactId,
            SourceCode = ExtractTransferSourceCodeFromSplitDescription(split.Description),
            SplitAmount = split.Amount
        };
    }

    private static string? ExtractTransferSourceCodeFromSplitDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var trimmed = description.Trim();
        var colonIndex = trimmed.IndexOf(':');
        return colonIndex > 0 ? trimmed[..colonIndex].Trim() : trimmed;
    }

    private async Task<Guid?> ResolveDepositSplitPaymentLineIdAsync(Guid organizationId, Deposit deposit, DepositSplit split)
    {
        if (deposit.OfficeId <= 0)
            return null;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(deposit.OrganizationId, deposit.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, deposit.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0)
            return null;

        if (split.JournalEntryLineId is { } existingLineId
            && existingLineId != Guid.Empty
            && await IsValidDepositSplitJournalEntryLineAsync(split, undepositedFundsAccountId))
        {
            return existingLineId;
        }

        var paymentLineCandidates = await BuildUndepositedPaymentLineCandidatesAsync(deposit, undepositedFundsAccountId);
        if (paymentLineCandidates.Count == 0)
            return null;

        var claimedLineIds = await GetJournalEntryLineIdsClaimedByOtherDepositsAsync(deposit);
        var assignedLineIds = (deposit.Splits ?? [])
            .Where(otherSplit => otherSplit != split
                && otherSplit.JournalEntryLineId is { } assignedLineId
                && assignedLineId != Guid.Empty)
            .Select(otherSplit => otherSplit.JournalEntryLineId!.Value)
            .ToHashSet();

        return ResolveDepositSplitJournalEntryLineId(
            deposit,
            split,
            paymentLineCandidates,
            claimedLineIds,
            assignedLineIds);
    }

    private async Task<TransferDepositAllocationScope?> BuildTransferDepositAllocationScopeFromSplitAsync(Guid organizationId, DepositSplit split, Guid splitJournalEntryLineId)
    {
        var splitSourceLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(splitJournalEntryLineId);
        if (splitSourceLine == null || splitSourceLine.JournalEntryId == Guid.Empty)
            return null;

        var splitSourceEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(splitSourceLine.JournalEntryId, organizationId);
        if (splitSourceEntry == null)
            return null;

        return new TransferDepositAllocationScope
        {
            PropertyId = split.PropertyId ?? splitSourceLine.PropertyId,
            ReservationId = split.ReservationId ?? splitSourceLine.ReservationId,
            ContactId = split.ContactId ?? splitSourceLine.ContactId,
            SourceCode = splitSourceEntry.SourceCode,
            PaymentId = splitSourceEntry.PaymentId,
            InvoiceId = splitSourceEntry.SourceTypeId == (int)SourceType.Invoice ? splitSourceEntry.SourceId : null,
            SplitAmount = split.Amount
        };
    }

    private async Task<List<JournalEntry>> LoadScopedTransferDepositJournalEntriesAsync(Guid organizationId, Guid depositId, IReadOnlyList<JournalEntry> depositJournalEntries, TransferDepositAllocationScope? allocationScope)
    {
        if (allocationScope == null)
            return depositJournalEntries.ToList();

        var scopedDepositJournalEntries = depositJournalEntries
            .Where(entry => MatchesTransferDepositAllocationScope(entry, allocationScope))
            .ToList();

        if (allocationScope.PaymentId is not { } scopedPaymentId || scopedPaymentId == Guid.Empty)
            return scopedDepositJournalEntries;

        var paymentJournalEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria
        {
            OrganizationId = organizationId,
            PaymentId = scopedPaymentId
        })).ToList();

        foreach (var paymentJournalEntry in paymentJournalEntries)
        {
            if (paymentJournalEntry.DepositId != depositId)
                continue;

            if (scopedDepositJournalEntries.Any(entry => entry.JournalEntryId == paymentJournalEntry.JournalEntryId))
                continue;

            if (MatchesTransferDepositAllocationScope(paymentJournalEntry, allocationScope))
                scopedDepositJournalEntries.Add(paymentJournalEntry);
        }

        return scopedDepositJournalEntries;
    }

    private async Task<(decimal OwnerEscrow, decimal SecDep, decimal Sdw, decimal Fee, Guid? PropertyId, Guid? ReservationId, Guid? ContactId, string Description)> ClassifyTransferDepositAllocationAsync(Guid organizationId, int officeId, IReadOnlyList<JournalEntry> scopedDepositJournalEntries, TransferDepositAllocationScope? allocationScope, TransferDepositRecapAccountContext recapContext)
    {
        var ownerEscrow = 0m;
        var secDep = 0m;
        var sdw = 0m;
        var fee = 0m;
        Guid? propertyId = allocationScope?.PropertyId;
        Guid? reservationId = allocationScope?.ReservationId;
        Guid? contactId = allocationScope?.ContactId;
        var description = allocationScope?.SourceCode ?? string.Empty;

        foreach (var entry in scopedDepositJournalEntries)
        {
            AccumulateTransferDepositClassification(
                entry,
                recapContext,
                ref ownerEscrow,
                ref secDep,
                ref sdw,
                ref fee,
                includeOwnerEscrow: true);

            var contextLine = (entry.JournalEntryLines ?? []).FirstOrDefault();
            if (contextLine != null)
            {
                propertyId ??= contextLine.PropertyId;
                reservationId ??= contextLine.ReservationId;
                contactId ??= contextLine.ContactId;
            }

            if (string.IsNullOrWhiteSpace(description))
                description = entry.SourceCode ?? entry.Memo ?? string.Empty;
        }

        var invoiceIds = allocationScope?.InvoiceId is { } scopedInvoiceId && scopedInvoiceId != Guid.Empty
            ? [scopedInvoiceId]
            : scopedDepositJournalEntries
                .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice && entry.SourceId is { } sourceId && sourceId != Guid.Empty)
                .Select(entry => entry.SourceId!.Value)
                .Distinct()
                .ToList();

        foreach (var invoiceId in invoiceIds)
        {
            var chargeEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(),
                SourceTypeId = (int)SourceType.Invoice,
                SourceId = invoiceId,
                IncludeUnposted = true
            }))
                .Where(entry => entry.JournalEntryKindId == JournalEntryKind.Charge)
                .ToList();

            foreach (var entry in chargeEntries)
            {
                AccumulateTransferDepositClassification(
                    entry,
                    recapContext,
                    ref ownerEscrow,
                    ref secDep,
                    ref sdw,
                    ref fee,
                    includeOwnerEscrow: false);
            }
        }

        return (
            RoundCurrency(ownerEscrow),
            RoundCurrency(secDep),
            RoundCurrency(sdw),
            RoundCurrency(fee),
            propertyId,
            reservationId,
            contactId,
            description);
    }

    private static void AccumulateTransferDepositClassification(JournalEntry entry, TransferDepositRecapAccountContext recapContext, ref decimal ownerEscrow, ref decimal secDep, ref decimal sdw, ref decimal fee, bool includeOwnerEscrow)
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
                case "SecurityDeposit":
                    secDep = RoundCurrency(secDep + classification.Amount);
                    break;
                case "SDW":
                    sdw = RoundCurrency(sdw + classification.Amount);
                    break;
                case "Fee":
                    fee = RoundCurrency(fee + classification.Amount);
                    break;
            }
        }
    }

    private static TransferDepositAllocationResult BuildTransferDepositAllocationResult(Guid depositId, Guid? journalEntryLineId, decimal escrowAmount, Deposit? deposit, TransferDepositAllocationScope? allocationScope, decimal ownerEscrow, decimal secDep, decimal sdw, decimal fee, Guid? propertyId, Guid? reservationId, Guid? contactId, string description)
    {
        var normalizedEscrowAmount = RoundCurrency(escrowAmount);
        var fullDepositEscrowAmount = allocationScope?.SplitAmount is decimal splitAmount && splitAmount != 0
            ? RoundCurrency(Math.Abs(splitAmount))
            : RoundCurrency(deposit?.Amount ?? normalizedEscrowAmount);

        if (fullDepositEscrowAmount != 0
            && Math.Abs(normalizedEscrowAmount - fullDepositEscrowAmount) > 0.005m)
        {
            var ratio = normalizedEscrowAmount / fullDepositEscrowAmount;
            ownerEscrow = RoundCurrency(ownerEscrow * ratio);
            secDep = RoundCurrency(secDep * ratio);
            sdw = RoundCurrency(sdw * ratio);
            fee = RoundCurrency(fee * ratio);
        }

        var business = RoundCurrency(normalizedEscrowAmount - ownerEscrow - secDep - sdw - fee);
        var drift = RoundCurrency(normalizedEscrowAmount - (ownerEscrow + secDep + sdw + fee + business));
        if (drift != 0)
            business = RoundCurrency(business + drift);

        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
            JournalEntryLineId = journalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null,
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

    private static bool MatchesTransferDepositAllocationScope(JournalEntry entry, TransferDepositAllocationScope scope)
    {
        if (entry.SourceTypeId == (int)SourceType.Deposit)
            return false;

        if (scope.PaymentId is { } paymentId && paymentId != Guid.Empty && entry.PaymentId == paymentId)
            return true;

        if (scope.InvoiceId is { } invoiceId
            && invoiceId != Guid.Empty
            && entry.SourceTypeId == (int)SourceType.Invoice
            && entry.SourceId == invoiceId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(scope.SourceCode)
            && string.Equals(entry.SourceCode, scope.SourceCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!scope.PropertyId.HasValue && !scope.ReservationId.HasValue)
            return false;

        return entry.JournalEntryLines?.Any(line => JournalEntryLineMatchesTransferDepositAllocationScope(line, scope)) == true;
    }

    private static bool JournalEntryLineMatchesTransferDepositAllocationScope(JournalEntryLine line, TransferDepositAllocationScope scope)
    {
        if (scope.PropertyId is { } propertyId
            && propertyId != Guid.Empty
            && line.PropertyId != propertyId)
        {
            return false;
        }

        if (scope.ReservationId is { } reservationId
            && reservationId != Guid.Empty
            && line.ReservationId != reservationId)
        {
            return false;
        }

        return scope.PropertyId is { } matchedPropertyId && matchedPropertyId != Guid.Empty
            || scope.ReservationId is { } matchedReservationId && matchedReservationId != Guid.Empty;
    }

    public async Task<IReadOnlyList<TransferReportLineAllocationResult>> ResolveTransferReportLineAllocationsAsync(Guid organizationId, Guid transferId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        if (transferId == Guid.Empty)
            throw new ArgumentException("TransferId is required.", nameof(transferId));

        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found");

        await EnrichTransferSplitsFromJournalEntryLinesAsync(transfer);

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, transfer.OfficeId);
        var recapContext = BuildTransferDepositRecapAccountContext(chartOfAccounts, transfer.OfficeId, accountingOffice);
        var results = new List<TransferReportLineAllocationResult>();

        var groups = (transfer.Splits ?? [])
            .Where(split => split.JournalEntryLineId is { } lineId && lineId != Guid.Empty)
            .GroupBy(split => split.JournalEntryLineId!.Value);

        foreach (var group in groups)
        {
            var journalEntryLineId = group.Key;
            var sourceLine = await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId)
                ?? throw new Exception("Transfer split journal entry line not found.");

            var depositJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(sourceLine.JournalEntryId, organizationId)
                ?? throw new Exception("Journal entry not found for transfer split line.");

            if (depositJournalEntry.DepositId is not { } depositId || depositId == Guid.Empty)
                throw new Exception("Transfer split journal entry line must belong to a journal entry with a deposit link.");

            var contextSplit = group.First();
            var sourceAmount = contextSplit.SourceJournalEntryLineAmount;
            var escrowAmount = sourceAmount.HasValue && sourceAmount.Value != 0
                ? RoundCurrency(sourceAmount.Value)
                : RoundCurrency(group.Sum(split => split.Amount));

            var allocation = await ResolveTransferDepositAllocationAsync(
                organizationId,
                transfer.OfficeId,
                depositId,
                escrowAmount,
                recapContext,
                journalEntryLineId);

            results.Add(new TransferReportLineAllocationResult
            {
                JournalEntryLineId = journalEntryLineId,
                DepositId = depositId,
                EscrowAmount = escrowAmount,
                OwnerEscrow = allocation.OwnerEscrow,
                SecDep = allocation.SecDep,
                Sdw = allocation.Sdw,
                Business = allocation.Business,
                PropertyId = allocation.PropertyId ?? contextSplit.PropertyId,
                ReservationId = allocation.ReservationId ?? contextSplit.ReservationId,
                ContactId = allocation.ContactId ?? contextSplit.ContactId,
                Description = allocation.Description
            });
        }

        return results;
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
            IsRentalIncomeAccount = recapContext.RentalIncomeAccountIds.Contains(line.ChartOfAccountId),
            IsCashOnly = entry.IsCashOnly
        };
    }

    private static decimal RoundCurrency(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}

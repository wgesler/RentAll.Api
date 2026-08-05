using RentAll.Domain.Accounting;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private sealed class TransferDepositRecapAccountContext
    {
        public int AccountsReceivableAccountId { get; init; }
        public int UndepositedFundsAccountId { get; init; }
        public int PrePaymentAccountId { get; init; }
        public int OwnerAccountsPayableAccountId { get; init; }
        public int OwnerExpenseAccountId { get; init; }
        public int TenantIncomeAccountId { get; init; }
        public HashSet<int> RentalIncomeAccountIds { get; init; } = [];
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

        foreach (var item in items)
        {
            if (item.DepositId == Guid.Empty)
                continue;

            results.Add(await ResolveTransferDepositAllocationAsync(organizationId, officeId, item.DepositId, item.EscrowAmount, recapContext));
        }

        return results;
    }

    private async Task<TransferDepositAllocationResult> ResolveTransferDepositAllocationAsync(Guid organizationId, int officeId, Guid depositId, decimal escrowAmount, TransferDepositRecapAccountContext recapContext)
    {
        var depositJournalEntries = (await _journalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria
        {
            OrganizationId = organizationId,
            DepositId = depositId
        })).ToList();

        var ownerEscrow = 0m;
        var secDep = 0m;
        var sdw = 0m;
        var fee = 0m;
        Guid? propertyId = null;
        Guid? reservationId = null;
        Guid? contactId = null;
        var description = string.Empty;

        foreach (var entry in depositJournalEntries)
        {
            foreach (var line in entry.JournalEntryLines)
            {
                if (!JournalEntryRecapLineClassifier.TryClassify(
                        BuildTransferDepositRecapClassificationLine(entry, line, recapContext),
                        out var classification))
                {
                    continue;
                }

                switch (classification.RecapCategory)
                {
                    case "OwnerRentActual":
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

            var contextLine = entry.JournalEntryLines.FirstOrDefault();
            if (contextLine != null)
            {
                propertyId ??= NormalizeOptionalGuid(contextLine.PropertyId);
                reservationId ??= NormalizeOptionalGuid(contextLine.ReservationId);
                contactId ??= NormalizeOptionalGuid(contextLine.ContactId);
            }

            if (string.IsNullOrWhiteSpace(description))
                description = (entry.SourceCode ?? entry.Memo ?? string.Empty).Trim();
        }

        var invoiceIds = depositJournalEntries
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
                foreach (var line in entry.JournalEntryLines)
                {
                    if (!JournalEntryRecapLineClassifier.TryClassify(
                            BuildTransferDepositRecapClassificationLine(entry, line, recapContext),
                            out var classification))
                    {
                        continue;
                    }

                    switch (classification.RecapCategory)
                    {
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
        }

        ownerEscrow = RoundCurrency(ownerEscrow);
        secDep = RoundCurrency(secDep);
        sdw = RoundCurrency(sdw);
        fee = RoundCurrency(fee);
        var normalizedEscrowAmount = RoundCurrency(escrowAmount);
        var business = RoundCurrency(normalizedEscrowAmount - ownerEscrow - secDep - sdw - fee);
        var drift = RoundCurrency(normalizedEscrowAmount - (ownerEscrow + secDep + sdw + fee + business));
        if (drift != 0)
            business = RoundCurrency(business + drift);

        return new TransferDepositAllocationResult
        {
            DepositId = depositId,
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
                recapContext);

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
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class TransferReportAccountIds
    {
        public int? OwnersAccountId { get; init; }
        public int? SecDepAccountId { get; init; }
        public int? SdwAccountId { get; init; }
        public int? BankAccountId { get; init; }
    }

    public async Task<TransferReport> GetTransferReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = true,
            IncludeInactive = false,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        }))
            .Where(transfer => transfer.IsActive)
            .ToList();

        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        var accountIdsByOffice = await LoadTransferReportAccountIdsByOfficeAsync(criteria.OrganizationId, officeIds);
        var journalEntriesByTransferId = await LoadTransferJournalEntriesByTransferIdAsync(criteria.OrganizationId, transfers);
        var journalEntryCodesById = await LoadJournalEntryCodesByIdAsync(
            criteria.OrganizationId,
            journalEntriesByTransferId.Values.Select(entry => entry.JournalEntryId));

        var rows = transfers
            .Select(transfer =>
            {
                accountIdsByOffice.TryGetValue(transfer.OfficeId, out var accountIds);
                accountIds ??= new TransferReportAccountIds();
                journalEntriesByTransferId.TryGetValue(transfer.TransferId, out var journalEntry);
                string? journalEntryCode = null;
                if (journalEntry != null)
                    journalEntryCodesById.TryGetValue(journalEntry.JournalEntryId, out journalEntryCode);
                return BuildTransferReportRowFromTransfer(transfer, accountIds, journalEntry?.JournalEntryId, journalEntryCode);
            })
            .Where(row => row != null && HasTransferReportMeaningfulAmount(row!))
            .Cast<TransferReportRow>()
            .OrderBy(row => row.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ReservationCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.SortDateValue)
            .ThenBy(row => row.Source, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TransferReport
        {
            Rows = rows
        };
    }

    private async Task<Dictionary<int, TransferReportAccountIds>> LoadTransferReportAccountIdsByOfficeAsync(
        Guid organizationId,
        IReadOnlyCollection<int> officeIds)
    {
        var accountIdsByOffice = new Dictionary<int, TransferReportAccountIds>();
        foreach (var officeId in officeIds)
        {
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
            accountIdsByOffice[officeId] = new TransferReportAccountIds
            {
                OwnersAccountId = accountingOffice?.DefaultEscrowOwnersAccountId,
                SecDepAccountId = accountingOffice?.DefaultEscrowSecDepAccountId,
                SdwAccountId = accountingOffice?.DefaultEscrowSdwAccountId,
                BankAccountId = accountingOffice?.DefaultBankAccountId
            };
        }

        return accountIdsByOffice;
    }

    private async Task<Dictionary<Guid, string>> LoadJournalEntryCodesByIdAsync(
        Guid organizationId,
        IEnumerable<Guid> journalEntryIds)
    {
        var codesById = new Dictionary<Guid, string>();
        foreach (var journalEntryId in journalEntryIds.Distinct())
        {
            var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
            if (journalEntry == null)
                continue;

            var code = (journalEntry.JournalEntryCode ?? journalEntry.SourceCode ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(code))
                codesById[journalEntryId] = code;
        }

        return codesById;
    }

    private async Task<Dictionary<Guid, JournalEntry>> LoadTransferJournalEntriesByTransferIdAsync(
        Guid organizationId,
        IReadOnlyList<Transfer> transfers)
    {
        var journalEntriesByTransferId = new Dictionary<Guid, JournalEntry>();
        foreach (var transfer in transfers)
        {
            var entries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = transfer.OfficeId.ToString(),
                SourceTypeId = (int)SourceType.Transfer,
                SourceId = transfer.TransferId,
                IncludeVoided = true,
                IncludeUnposted = true
            })).ToList();

            var entry = entries.FirstOrDefault();
            if (entry != null)
                journalEntriesByTransferId[transfer.TransferId] = entry;
        }

        return journalEntriesByTransferId;
    }

    private static TransferReportRow? BuildTransferReportRowFromTransfer(
        Transfer transfer,
        TransferReportAccountIds accountIds,
        Guid? journalEntryId,
        string? journalEntryCode)
    {
        var splits = transfer.Splits ?? [];
        var ownerRentActualValue = SumTransferSplitAmountsForAccount(splits, accountIds.OwnersAccountId);
        var securityDepositValue = SumTransferSplitAmountsForAccount(splits, accountIds.SecDepAccountId);
        var sdwValue = SumTransferSplitAmountsForAccount(splits, accountIds.SdwAccountId);
        var businessValue = SumTransferSplitAmountsForAccount(splits, accountIds.BankAccountId);
        var expectedIncomeValue = RoundCurrency(
            ownerRentActualValue + securityDepositValue + sdwValue + businessValue);
        var contextSplit = splits.FirstOrDefault(split => split.PropertyId.HasValue && split.PropertyId != Guid.Empty)
            ?? splits.FirstOrDefault();
        var source = ResolveTransferReportSource(transfer);
        var resolvedJournalEntryCode = (journalEntryCode ?? transfer.TransferCode ?? string.Empty).Trim();
        var transactionDate = transfer.TransferDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sortDateValue = transfer.TransferDate.ToDateTime(TimeOnly.MinValue).Ticks;

        return new TransferReportRow
        {
            PropertyCode = (contextSplit?.PropertyCode ?? string.Empty).Trim(),
            ReservationCode = (contextSplit?.ReservationCode ?? string.Empty).Trim(),
            AccountingPeriod = FormatJournalEntryRecapAccountingPeriod(transfer.AccountingPeriod.ToString("yyyy-MM-dd")),
            Source = source,
            JournalEntryCode = resolvedJournalEntryCode,
            SourceTypeId = (int)SourceType.Transfer,
            SourceId = transfer.TransferId,
            SourceLinkable = true,
            ActivityType = "Transfer",
            OfficeId = transfer.OfficeId,
            PropertyId = contextSplit?.PropertyId ?? transfer.PropertyId,
            ReservationId = contextSplit?.ReservationId,
            TransactionDate = transactionDate,
            ExpectedIncome = FormatCurrencyUsd(expectedIncomeValue),
            RentPlus4000 = FormatCurrencyUsd(ownerRentActualValue),
            OwnerRent = FormatCurrencyUsd(ownerRentActualValue),
            OwnerRentActual = FormatCurrencyUsd(ownerRentActualValue),
            Business = FormatCurrencyUsd(businessValue),
            SecurityDeposit = FormatCurrencyUsd(securityDepositValue),
            Sdw = FormatCurrencyUsd(sdwValue),
            Fee = FormatCurrencyUsd(businessValue),
            ExpectedIncomeValue = expectedIncomeValue,
            RentPlus4000Value = ownerRentActualValue,
            OwnerRentValue = ownerRentActualValue,
            OwnerRentActualValue = ownerRentActualValue,
            BusinessValue = businessValue,
            SecurityDepositValue = securityDepositValue,
            SdwValue = sdwValue,
            FeeValue = businessValue,
            SortDateValue = sortDateValue,
            JournalEntryId = journalEntryId,
            JournalEntryLineId = null
        };
    }

    private static decimal SumTransferSplitAmountsForAccount(IEnumerable<TransferSplit> splits, int? accountId)
    {
        if (accountId is null or <= 0)
            return 0;

        return RoundCurrency(splits
            .Where(split => split.ChartOfAccountId == accountId)
            .Sum(split => split.Amount));
    }

    private static string ResolveTransferReportSource(Transfer transfer)
    {
        foreach (var split in transfer.Splits ?? [])
        {
            var description = (split.Description ?? string.Empty).Trim();
            var transferPrefixMatch = System.Text.RegularExpressions.Regex.Match(description, @"^Transfer\s+(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (transferPrefixMatch.Success && !string.IsNullOrWhiteSpace(transferPrefixMatch.Groups[1].Value))
                return transferPrefixMatch.Groups[1].Value.Trim();
        }

        var transferDescription = (transfer.Description ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(transferDescription)
            && !transferDescription.Equals("transfer", StringComparison.OrdinalIgnoreCase))
        {
            return transferDescription;
        }

        return (transfer.TransferCode ?? string.Empty).Trim();
    }

    private static List<int> ParseOfficeIds(string officeIds)
        => (officeIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool HasTransferReportMeaningfulAmount(TransferReportRow row) =>
        row.ExpectedIncomeValue != 0
        || row.RentPlus4000Value != 0
        || row.OwnerRentValue != 0
        || row.OwnerRentActualValue != 0
        || row.BusinessValue != 0
        || row.SecurityDepositValue != 0
        || row.SdwValue != 0
        || row.FeeValue != 0;
}

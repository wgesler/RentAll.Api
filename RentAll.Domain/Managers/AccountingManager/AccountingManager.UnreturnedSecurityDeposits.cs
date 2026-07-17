using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<UnreturnedSecurityDepositsResult> GetUnreturnedSecurityDepositsAsync(
        Guid organizationId,
        string officeAccess,
        int? officeId = null)
    {
        var rows = (await _reservationRepository.GetUnreturnedSecurityDepositsAsync(organizationId, officeAccess)).ToList();
        if (officeId is > 0)
            rows = rows.Where(row => row.OfficeId == officeId.Value).ToList();

        var officeIds = ResolveSecurityDepositSummaryOfficeIds(officeAccess, officeId, rows);
        var totalDepositsOwed = RoundSecurityDepositAmount(rows.Sum(row => row.Deposit));

        decimal escrowBalance = 0m;
        var escrowAccountLabels = new List<string>();
        foreach (var summaryOfficeId in officeIds)
        {
            var (accountLabel, balance) = await LoadEscrowSecurityDepositAccountBalanceAsync(organizationId, summaryOfficeId);
            escrowBalance = RoundSecurityDepositAmount(escrowBalance + balance);
            if (!string.IsNullOrWhiteSpace(accountLabel))
                escrowAccountLabels.Add(accountLabel.Trim());
        }

        return new UnreturnedSecurityDepositsResult
        {
            Rows = rows,
            TotalDepositsOwed = totalDepositsOwed,
            EscrowBalance = escrowBalance,
            Discrepancy = RoundSecurityDepositAmount(escrowBalance - totalDepositsOwed),
            EscrowAccountLabel = ResolveEscrowAccountLabel(escrowAccountLabels, officeIds.Count)
        };
    }

    private static List<int> ResolveSecurityDepositSummaryOfficeIds(
        string officeAccess,
        int? officeId,
        IReadOnlyList<ReservationDeparture> rows)
    {
        if (officeId is > 0)
            return [officeId.Value];

        var accessOfficeIds = ParseOfficeIdsFromAccess(officeAccess);
        if (accessOfficeIds.Count > 0)
            return accessOfficeIds;

        return rows
            .Select(row => row.OfficeId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static string ResolveEscrowAccountLabel(IReadOnlyList<string> accountLabels, int officeCount)
    {
        if (accountLabels.Count == 1)
            return accountLabels[0];

        if (accountLabels.Count > 1)
            return "Escrow Security Deposit";

        return officeCount == 1 ? "Escrow Security Deposit" : "Escrow Security Deposit";
    }

    private async Task<(string AccountLabel, decimal Balance)> LoadEscrowSecurityDepositAccountBalanceAsync(
        Guid organizationId,
        int officeId)
    {
        try
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            var accountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, officeId, accountingOffice);
            var account = chartOfAccounts.FirstOrDefault(item => item.AccountId == accountId);
            var accountLabel = FormatEscrowSecurityDepositAccountLabel(account);

            var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                ChartOfAccountId = accountId,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = null,
                EndDate = null
            });

            var balance = RoundSecurityDepositAmount(SumEscrowLiabilityAccountBalance(lines));
            return (accountLabel, balance);
        }
        catch
        {
            return (string.Empty, 0m);
        }
    }

    private static decimal SumEscrowLiabilityAccountBalance(IEnumerable<JournalEntryLineSearchResult> lines)
        => (lines ?? []).Sum(line => line.Credit - line.Debit);

    private static string FormatEscrowSecurityDepositAccountLabel(ChartOfAccount? account)
    {
        if (account == null)
            return string.Empty;

        return $"{account.AccountNo} {account.Name}".Trim();
    }

    private static decimal RoundSecurityDepositAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static List<int> ParseOfficeIdsFromAccess(string officeAccess)
        => (officeAccess ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();
}

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<string> GetRentalIncomeParentAccountIdsAsync(Guid organizationId, string officeIds)
    {
        var parents = await ResolveRentalIncomeParentAccountsAsync(organizationId, officeIds);
        return string.Join(',', parents.Select(parent => parent.AccountId));
    }

    public async Task<(string AccountIds, string AccountNos)> GetRentalIncomeParentAccountsAsync(Guid organizationId, string officeIds)
    {
        var parents = await ResolveRentalIncomeParentAccountsAsync(organizationId, officeIds);
        var accountIds = string.Join(',', parents.Select(parent => parent.AccountId));
        var accountNos = string.Join(',', parents
            .Select(parent => parent.AccountNo)
            .Where(accountNo => !string.IsNullOrWhiteSpace(accountNo))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        return (accountIds, accountNos);
    }

    private async Task<List<(int AccountId, string AccountNo)>> ResolveRentalIncomeParentAccountsAsync(Guid organizationId, string officeIds)
    {
        var distinctOfficeIds = (officeIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();

        if (distinctOfficeIds.Count == 0)
            return [];

        var parentsByAccountId = new Dictionary<int, string>();
        foreach (var officeId in distinctOfficeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            var (office, costCodeById) = await LoadOfficeCostCodeContextAsync(organizationId, officeId);
            var parentAccountId = GetDefaultParentRentalIncomeAccount(chartOfAccounts, officeId, office, costCodeById, accountingOffice);
            if (parentsByAccountId.ContainsKey(parentAccountId))
                continue;

            var accountNo = chartOfAccounts
                .FirstOrDefault(account => account.OfficeId == officeId && account.AccountId == parentAccountId)
                ?.AccountNo?
                .Trim() ?? string.Empty;
            parentsByAccountId[parentAccountId] = accountNo;
        }

        return parentsByAccountId
            .Select(pair => (pair.Key, pair.Value))
            .ToList();
    }
}

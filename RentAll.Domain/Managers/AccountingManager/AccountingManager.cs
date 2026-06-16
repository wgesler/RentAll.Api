using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager : IAccountingManager
{
    private readonly Guid SystemOrganization = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IOrganizationManager _organizationManager;
    private readonly IFeatureFlagService _featureFlagService;

    public AccountingManager(
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository,
        IAccountingRepository accountingRepository,
        IMaintenanceRepository maintenanceRepository,
        IReservationRepository reservationRepository,
        IJournalEntryRepository journalEntryRepository,
        IOrganizationManager organizationManager,
        IFeatureFlagService featureFlagService)
    {
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
        _accountingRepository = accountingRepository;
        _maintenanceRepository = maintenanceRepository;
        _reservationRepository = reservationRepository;
        _journalEntryRepository = journalEntryRepository;
        _organizationManager = organizationManager;
        _featureFlagService = featureFlagService;
    }

    private Task<bool> IsAccountingFeatureEnabledAsync(Guid organizationId)
        => _featureFlagService.IsEnabledAsync(FeatureFlagKeys.Accounting, organizationId);

    #region Account Context
    private async Task<(List<ChartOfAccount> ChartOfAccounts, AccountingOffice? AccountingOffice)> LoadAccountContextAsync(Guid organizationId, int officeId)
    {
        var chartOfAccountsTask = _accountingRepository.GetChartOfAccountsByOfficeIdAsync(organizationId, officeId);
        var accountingOfficeTask = _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);

        await Task.WhenAll(chartOfAccountsTask, accountingOfficeTask);

        return (await chartOfAccountsTask, await accountingOfficeTask);
    }
    #endregion

    #region Account Helpers
    private static int? TryGetConfiguredAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, int? configuredAccountId)
    {
        if (configuredAccountId is not > 0)
            return null;

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.AccountId == configuredAccountId.Value && a.OfficeId == officeId);

        return account?.AccountId;
    }

    private static int GetDefaultOrByNameOrTypeAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, int? configuredAccountId, Func<int> byNameOrType)
    {
        var configured = TryGetConfiguredAccountId(chartOfAccounts, officeId, configuredAccountId);
        if (configured is > 0)
            return configured.Value;

        return byNameOrType();
    }

    private static int FindAccountIdByType(List<ChartOfAccount> chartOfAccounts, int officeId, AccountType accountType, string accountLabel)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == accountType)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No {accountLabel} chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int FindAccountIdByTypeAndName(List<ChartOfAccount> chartOfAccounts, int officeId, AccountType accountType, string nameContains, string accountLabel)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == accountType)
            .Where(a => a.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == accountType)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No {accountLabel} chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static string NormalizeAccountCode(string value)
    {
        return string.Join(' ',
            value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static bool IsUndepositedFundsChartOfAccount(ChartOfAccount account)
    {
        return account.AccountType == AccountType.OtherCurrentAsset
            && account.Name.Contains("Undeposited", StringComparison.OrdinalIgnoreCase);
    }
    #endregion

    #region AccountsReceivable Account
    private static int? GetDefaultAccountsReceivableAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultActRecvAccountId is > 0 ? accountingOffice.DefaultActRecvAccountId : null;

    private static int GetAccountsReceivableAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByType(chartOfAccounts, officeId, AccountType.AccountsReceivable, "Accounts Receivable");

    private static int GetAccountsReceivableAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultAccountsReceivableAccountId(accountingOffice), () => GetAccountsReceivableAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region Deposit Account
    private static int? GetDefaultDepositAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultEscrowAccountId is > 0 ? accountingOffice.DefaultEscrowAccountId : null;

    private static int GetDepositAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentLiability)
            .Where(a =>
                a.Name.Contains("Escrow", StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("Deposit", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentLiability)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Escrow chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDepositAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultDepositAccountId(accountingOffice), () => GetDepositAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region UndepositedFunds Account
    private static int? GetDefaultUndepositedFundsAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultUndepFundsAccountId is > 0 ? accountingOffice.DefaultUndepFundsAccountId : null;

    private static int GetUndepositedFundsAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentAsset)
            .Where(a => a.Name.Contains("Undeposited", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentAsset)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Undeposited Funds chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetUndepositedFundsAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultUndepositedFundsAccountId(accountingOffice), () => GetUndepositedFundsAccountIdByNameOrType(chartOfAccounts, officeId));

    private static HashSet<int> GetUndepositedFundsAccountIds(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        var configuredAccountId = TryGetConfiguredAccountId(chartOfAccounts, officeId, GetDefaultUndepositedFundsAccountId(accountingOffice));

        if (configuredAccountId is > 0)
            return new HashSet<int> { configuredAccountId.Value };

        var accountIds = chartOfAccounts
            .Where(a => a.OfficeId == officeId && IsUndepositedFundsChartOfAccount(a))
            .Select(a => a.AccountId)
            .ToHashSet();

        if (accountIds.Count == 0)
            throw new Exception($"No Undeposited Funds chart of account is configured for office {officeId}");

        return accountIds;
    }
    #endregion

    #region Bank Account
    private static int? GetDefaultBankAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultBankAccountId is > 0 ? accountingOffice.DefaultBankAccountId : null;

    private static int GetBankAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByType(chartOfAccounts, officeId, AccountType.Bank, "Bank");

    private static int GetBankAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultBankAccountId(accountingOffice), () => GetBankAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region AccountsPayable Account
    private static int? GetDefaultAccountsPayableAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultActPayableAccountId is > 0 ? accountingOffice.DefaultActPayableAccountId : null;

    private static int GetAccountsPayableAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByType(chartOfAccounts, officeId, AccountType.AccountsPayable, "Accounts Payable");

    private static int GetAccountsPayableAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultAccountsPayableAccountId(accountingOffice), () => GetAccountsPayableAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region OwnerAccountsPayable Account
    private static int? GetDefaultOwnerAccountsPayableAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultOwnActPayableAccountId is > 0 ? accountingOffice.DefaultOwnActPayableAccountId : null;

    private static int GetOwnerAccountsPayableAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByTypeAndName(chartOfAccounts, officeId, AccountType.AccountsPayable, "Owner", "Owner Accounts Payable");

    private static int GetOwnerAccountsPayableAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultOwnerAccountsPayableAccountId(accountingOffice), () => GetOwnerAccountsPayableAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region TenantIncome Account
    private static int? GetDefaultTenantIncomeAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultTenantIncAccountId is > 0 ? accountingOffice.DefaultTenantIncAccountId : null;

    private static int GetTenantIncomeAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByTypeAndName(chartOfAccounts, officeId, AccountType.Income, "Tenant", "Tenant Income");

    private static int GetTenantIncomeAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultTenantIncomeAccountId(accountingOffice), () => GetTenantIncomeAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region TenantExpense Account
    private static int? GetDefaultTenantExpenseAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultTenantExpAccountId is > 0 ? accountingOffice.DefaultTenantExpAccountId : null;

    private static int GetTenantExpenseAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByTypeAndName(chartOfAccounts, officeId, AccountType.Expense, "Tenant", "Tenant Expense");

    private static int GetTenantExpenseAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultTenantExpenseAccountId(accountingOffice), () => GetTenantExpenseAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region OwnerIncome Account
    private static int? GetDefaultOwnerIncomeAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultOwnerIncAccountId is > 0 ? accountingOffice.DefaultOwnerIncAccountId : null;

    private static int GetOwnerIncomeAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByTypeAndName(chartOfAccounts, officeId, AccountType.Income, "Owner", "Owner Income");

    private static int GetOwnerIncomeAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultOwnerIncomeAccountId(accountingOffice), () => GetOwnerIncomeAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region OwnerExpense Account
    private static int? GetDefaultOwnerExpenseAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultOwnerExpAccountId is > 0 ? accountingOffice.DefaultOwnerExpAccountId : null;

    private static int GetOwnerExpenseAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByTypeAndName(chartOfAccounts, officeId, AccountType.Expense, "Owner", "Owner Expense");

    private static int GetOwnerExpenseAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultOwnerExpenseAccountId(accountingOffice), () => GetOwnerExpenseAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region CompanyExpense Account
    private static int? GetDefaultCompanyExpenseAccountId(AccountingOffice? accountingOffice)
        => accountingOffice?.DefaultCompanyExpAccountId is > 0 ? accountingOffice.DefaultCompanyExpAccountId : null;

    private static int GetCompanyExpenseAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        static bool MatchesCompanyOrInventory(ChartOfAccount account) =>
            account.Name.Contains("Company", StringComparison.OrdinalIgnoreCase) ||
            account.Name.Contains("Inventory", StringComparison.OrdinalIgnoreCase);

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
            .Where(MatchesCompanyOrInventory)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Company Expense chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetCompanyExpenseAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
        => GetDefaultOrByNameOrTypeAccountId(chartOfAccounts, officeId, GetDefaultCompanyExpenseAccountId(accountingOffice), () => GetCompanyExpenseAccountIdByNameOrType(chartOfAccounts, officeId));
    #endregion

    #region Other Chart Accounts
    private static int GetCostOfGoodsSoldAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByType(chartOfAccounts, officeId, AccountType.CostOfGoodsSold, "Cost of Goods Sold");

    private static int GetCreditCardAccountIdByNameOrType(List<ChartOfAccount> chartOfAccounts, int officeId)
        => FindAccountIdByType(chartOfAccounts, officeId, AccountType.CreditCard, "Credit Card");
    #endregion

    #region Journal Entry Account Resolution
    private static int GetDepositBankAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, int chartOfAccountId)
    {
        var account = chartOfAccounts.FirstOrDefault(a => a.AccountId == chartOfAccountId && a.OfficeId == officeId);

        if (account == null)
            throw new Exception("Invalid bank chart of account for deposit");

        if (account.AccountType != AccountType.Bank)
            throw new Exception("Deposit target account must be a bank account");

        return account.AccountId;
    }

    private static int GetChartOfAccountIdForCostCode(List<ChartOfAccount> chartOfAccounts, int officeId, CostCode? costCode, int defaultAccountId)
    {
        if (costCode == null || string.IsNullOrWhiteSpace(costCode.Code))
            return defaultAccountId;

        var accountCode = NormalizeAccountCode(costCode.Code);
        if (string.IsNullOrWhiteSpace(accountCode))
            return defaultAccountId;

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.OfficeId == officeId &&
            NormalizeAccountCode(a.AccountNo).Equals(accountCode, StringComparison.OrdinalIgnoreCase));

        return account?.AccountId ?? defaultAccountId;
    }

    private static int GetBillPaymentChartOfAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, int chartOfAccountId)
    {
        if (chartOfAccountId <= 0)
            throw new Exception("Chart of account is required for bill payment");

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.AccountId == chartOfAccountId && a.OfficeId == officeId);

        if (account == null)
            throw new Exception("Invalid chart of account for bill payment");

        if (account.AccountType != AccountType.Bank)
            throw new Exception("Bill payment offset account must be a bank account");

        return account.AccountId;
    }

    private static int GetExpenseOrCogsAccountId(List<ChartOfAccount> chartOfAccounts, int officeId, ReceiptSplit split, int defaultCostOfGoodsSoldAccountId, int defaultExpenseAccountId)
    {
        if (split.ChartOfAccountId is > 0)
        {
            var account = chartOfAccounts.FirstOrDefault(a =>
                a.AccountId == split.ChartOfAccountId.Value && a.OfficeId == officeId);

            if (account?.AccountType == AccountType.CostOfGoodsSold)
                return account.AccountId;

            if (account?.AccountType == AccountType.Expense)
                return account.AccountId;
        }

        if (defaultCostOfGoodsSoldAccountId > 0)
            return defaultCostOfGoodsSoldAccountId;

        return defaultExpenseAccountId;
    }

    private async Task<int> GetCreditCardAccountIdAsync(Receipt receipt, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        if (receipt.BankCardId is not > 0)
            throw new Exception("BankCardId is required to resolve a credit card account");

        var bankCard = await _accountingRepository.GetBankCardByIdAsync(
            receipt.BankCardId.Value,
            receipt.OrganizationId,
            receipt.OfficeId);

        if (bankCard == null)
            throw new Exception("Bank card not found");

        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(receipt.OrganizationId, receipt.OfficeId);
        var costCode = costCodes.FirstOrDefault(c => c.CostCodeId == bankCard.CostCodeId);
        var creditCardAccountId = GetCreditCardAccountIdByNameOrType(chartOfAccounts, receipt.OfficeId);
        return GetChartOfAccountIdForCostCode(chartOfAccounts, receipt.OfficeId, costCode, creditCardAccountId);
    }

    private async Task<int> GetBillLiabilityAccountIdAsync(Receipt bill, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        if (bill.BankCardId is > 0)
            return await GetCreditCardAccountIdAsync(bill, chartOfAccounts, accountingOffice);

        return GetAccountsPayableAccountId(chartOfAccounts, bill.OfficeId, accountingOffice);
    }
    #endregion
}

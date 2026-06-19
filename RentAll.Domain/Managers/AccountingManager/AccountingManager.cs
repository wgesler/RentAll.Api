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
    private readonly IContactRepository _contactRepository;
    private readonly IFeatureFlagService _featureFlagService;

    public AccountingManager(
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository,
        IAccountingRepository accountingRepository,
        IMaintenanceRepository maintenanceRepository,
        IReservationRepository reservationRepository,
        IJournalEntryRepository journalEntryRepository,
        IOrganizationManager organizationManager,
        IContactRepository contactRepository,
        IFeatureFlagService featureFlagService)
    {
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
        _accountingRepository = accountingRepository;
        _maintenanceRepository = maintenanceRepository;
        _reservationRepository = reservationRepository;
        _journalEntryRepository = journalEntryRepository;
        _organizationManager = organizationManager;
        _contactRepository = contactRepository;
        _featureFlagService = featureFlagService;
    }

    private Task<bool> IsAccountingFeatureEnabledAsync(Guid organizationId)
        => _featureFlagService.IsEnabledAsync(FeatureFlagKeys.Accounting, organizationId);

    #region Account Context
    private async Task<(List<ChartOfAccount> ChartOfAccounts, AccountingOffice? AccountingOffice)> LoadAccountContextAsync(Guid organizationId, int officeId)
    {
        var chartOfAccountsTask = _accountingRepository.GetChartOfAccountsByOfficeIdAsync(organizationId, officeId);
        var accountingOfficeTask = _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
        var bankCardsTask = _accountingRepository.GetBankCardsByOfficeIdAsync(organizationId, officeId);

        await Task.WhenAll(chartOfAccountsTask, accountingOfficeTask, bankCardsTask);

        var accountingOffice = await accountingOfficeTask;
        if (accountingOffice != null)
            accountingOffice.BankCards = await bankCardsTask;

        return (await chartOfAccountsTask, accountingOffice);
    }
    #endregion

    #region Default Chart Of Accounts
    // Tenant/Owner/Company Accounts
    private static int GetDefaultTenantIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultTenantIncAccountId is > 0)
            return accountingOffice.DefaultTenantIncAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
            .Where(a => a.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Tenant Income chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultTenantExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultTenantExpAccountId is > 0)
            return accountingOffice.DefaultTenantExpAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
            .Where(a => a.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Tenant Expense chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultOwnerIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultOwnerIncAccountId is > 0)
            return accountingOffice.DefaultOwnerIncAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
            .Where(a => a.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Owner Income chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultOwnerExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultOwnerExpAccountId is > 0)
            return accountingOffice.DefaultOwnerExpAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
            .Where(a => a.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Owner Expense chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultCompanyExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultCompanyExpAccountId is > 0)
            return accountingOffice.DefaultCompanyExpAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
            .Where(a =>
                a.Name.Contains("Company", StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("Inventory", StringComparison.OrdinalIgnoreCase))
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

    private static int GetDefaultDepartureIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultDepartureIncAccountId is > 0)
            return accountingOffice.DefaultDepartureIncAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
            .Where(a => a.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Departure Income chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultDepartureExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultDepartureExpAccountId is > 0)
            return accountingOffice.DefaultDepartureExpAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
            .Where(a => a.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Departure Expense chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    // Bank & Balance Sheet Accounts
    private static int GetDefaultBankAccount(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultBankAccountId is > 0)
            return accountingOffice.DefaultBankAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Bank)
            .Where(a => a.Name.Contains("Bank", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Bank)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Bank chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultAccountsReceivable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultActRecvAccountId is > 0)
            return accountingOffice.DefaultActRecvAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsReceivable)
            .Where(a => a.Name.Contains("Receivable", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsReceivable)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Accounts Receivable chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultDepositAccount(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultEscrowAccountId is > 0)
            return accountingOffice.DefaultEscrowAccountId.Value;

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

    private static int GetDefaultUndepositedFunds(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultUndepFundsAccountId is > 0)
            return accountingOffice.DefaultUndepFundsAccountId.Value;

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

    private static int GetDefaultAccountsPayable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultActPayableAccountId is > 0)
            return accountingOffice.DefaultActPayableAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsPayable)
            .Where(a => a.Name.Contains("Payable", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsPayable)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Accounts Payable chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultOwnerAccountsPayable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultOwnActPayableAccountId is > 0)
            return accountingOffice.DefaultOwnActPayableAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsPayable)
            .Where(a => a.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsPayable)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Owner Accounts Payable chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    private static int GetDefaultPrePayment(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultPrePayAccountId is > 0)
            return accountingOffice.DefaultPrePayAccountId.Value;

        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentLiability)
            .Where(a =>
                a.Name.Contains("Pre-Payment", StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("Prepayment", StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("Pre Payment", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentLiability)
                .OrderBy(a => a.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Pre-Payment chart of account is configured for office {officeId}");

        return account.AccountId;
    }
    #endregion

    #region Journal Entry Account Resolution
    private static int GetChartOfAccountIdByCostCode(List<ChartOfAccount> chartOfAccounts, int officeId, CostCode? costCode, int defaultAccountId)
    {
        if (costCode == null || string.IsNullOrWhiteSpace(costCode.Code))
            return defaultAccountId;

        var accountCode = NormalizeAccountCode(costCode.Code);
        if (string.IsNullOrWhiteSpace(accountCode))
            return defaultAccountId;

        var account = chartOfAccounts.FirstOrDefault(a => a.OfficeId == officeId &&
            NormalizeAccountCode(a.AccountNo).Equals(accountCode, StringComparison.OrdinalIgnoreCase));

        return account?.AccountId ?? defaultAccountId;
    }

    private int GetCreditCardAccountId(Receipt receipt, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        if (receipt.BankCardId is not > 0)
            throw new Exception("BankCardId is required to resolve a credit card account");

        var bankCard = accountingOffice?.BankCards.FirstOrDefault(card => card.BankCardId == receipt.BankCardId.Value);
        if (bankCard == null)
            throw new Exception("Bank card not found on accounting office");

        if (bankCard.ChartOfAccountId is not > 0)
            throw new Exception("Bank card must have a chart of account configured");

        return bankCard.ChartOfAccountId.Value;
    }

    private static int GetBillReceiptExpenseAccountId(ReceiptSplit split, int officeId, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        // If the split has a chart of account, use it
        if (split.ChartOfAccountId > 0)
            return split.ChartOfAccountId.Value;

        // Otherwise get defaults
        return split.ReceiptType switch
        {
            ReceiptType.Tenant => GetDefaultTenantExpense(chartOfAccounts, officeId, accountingOffice),
            ReceiptType.Departure => GetDefaultDepartureExpense(chartOfAccounts, officeId, accountingOffice),
            ReceiptType.Owner => GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice),
            _ => GetDefaultCompanyExpense(chartOfAccounts, officeId, accountingOffice)
        };
    }

    private int GetBillReceiptLiabilityAccountId(Receipt bill, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        if (bill.BankCardId is > 0)
            return GetCreditCardAccountId(bill, chartOfAccounts, accountingOffice);

        return GetDefaultAccountsPayable(chartOfAccounts, bill.OfficeId, accountingOffice);
    }
    #endregion

    #region Account Helpers
    private static string NormalizeAccountCode(string value)
    {
        return string.Join(' ',
            value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
    #endregion
}

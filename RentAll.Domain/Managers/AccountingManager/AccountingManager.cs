using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace RentAll.Domain.Managers;

public partial class AccountingManager : IAccountingManager
{
    private readonly record struct OfficeContextCacheKey(Guid OrganizationId, int OfficeId);
    private readonly record struct AccountResolverCacheKey(int ChartContextId, int OfficeId, string ResolverKey, string CostCodeKey);

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
    private readonly ConcurrentDictionary<OfficeContextCacheKey, Task<(List<ChartOfAccount> ChartOfAccounts, AccountingOffice? AccountingOffice)>> _accountContextCache = new();
    private readonly ConcurrentDictionary<OfficeContextCacheKey, Task<IReadOnlyDictionary<int, CostCode>>> _costCodeByOfficeCache = new();
    private readonly ConcurrentDictionary<AccountResolverCacheKey, int> _defaultAccountIdCache = new();

    public AccountingManager(IOrganizationRepository organizationRepository, IPropertyRepository propertyRepository, IAccountingRepository accountingRepository, IMaintenanceRepository maintenanceRepository, IReservationRepository reservationRepository, IJournalEntryRepository journalEntryRepository, IOrganizationManager organizationManager, IContactRepository contactRepository, IFeatureFlagService featureFlagService)
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

    #region Load Context
    private async Task<(List<ChartOfAccount> ChartOfAccounts, AccountingOffice? AccountingOffice)> LoadAccountContextAsync(Guid organizationId, int officeId)
    {
        var key = new OfficeContextCacheKey(organizationId, officeId);
        var accountContextTask = _accountContextCache.GetOrAdd(key, _ => FetchAccountContextAsync(organizationId, officeId));
        try
        {
            return await accountContextTask;
        }
        catch
        {
            _accountContextCache.TryRemove(key, out _);
            throw;
        }
    }

    private async Task<(List<ChartOfAccount> ChartOfAccounts, AccountingOffice? AccountingOffice)> FetchAccountContextAsync(Guid organizationId, int officeId)
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

    private async Task<(Office? Office, IReadOnlyDictionary<int, CostCode> CostCodeById)> LoadOfficeCostCodeContextAsync(Guid organizationId, int officeId)
    {
        var officeTask = _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        var costCodesTask = LoadCostCodeByOfficeIdAsync(organizationId, officeId);

        await Task.WhenAll(officeTask, costCodesTask);

        return (await officeTask, await costCodesTask);
    }

    private async Task<IReadOnlyDictionary<int, CostCode>> LoadCostCodeByOfficeIdAsync(Guid organizationId, int officeId)
    {
        var key = new OfficeContextCacheKey(organizationId, officeId);
        var costCodeTask = _costCodeByOfficeCache.GetOrAdd(key, _ => FetchCostCodeByOfficeIdAsync(organizationId, officeId));
        try
        {
            return await costCodeTask;
        }
        catch
        {
            _costCodeByOfficeCache.TryRemove(key, out _);
            throw;
        }
    }

    private async Task<IReadOnlyDictionary<int, CostCode>> FetchCostCodeByOfficeIdAsync(Guid organizationId, int officeId)
    {
        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(organizationId, officeId);
        return costCodes.ToDictionary(c => c.CostCodeId);
    }
    #endregion

    #region Default Chart Of Accounts
    // Office Cost Codes
    private int GetDefaultOfficeExpenseAccount(List<ChartOfAccount> chartOfAccounts, int officeId, int? officeExpenseCostCodeId, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        CostCode? costCode = null;
        if (officeExpenseCostCodeId is > 0)
            costCodeById.TryGetValue(officeExpenseCostCodeId.Value, out costCode);

        return GetDefaultTenantExpense(chartOfAccounts, officeId, accountingOffice, costCode);
    }

    private int GetDefaultFurnishedRentExpense(List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        return GetDefaultOfficeExpenseAccount(chartOfAccounts, officeId, office?.FurnishedRentExpenseCcId, costCodeById, accountingOffice);
    }

    private int GetDefaultUnfurnishedRentExpense(List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        return GetDefaultOfficeExpenseAccount(chartOfAccounts, officeId, office?.UnfurnishedRentExpenseCcId, costCodeById, accountingOffice);
    }

    private int GetDefaultMaidServiceExpense(List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        return GetDefaultOfficeExpenseAccount(chartOfAccounts, officeId, office?.MaidServiceExpenseCcId, costCodeById, accountingOffice);
    }

    private int GetDefaultParkingExpense(List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        return GetDefaultOfficeExpenseAccount(chartOfAccounts, officeId, office?.ParkingExpenseCcId, costCodeById, accountingOffice);
    }

    private int GetDefaultDepartureAccount(List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        return GetDefaultOfficeExpenseAccount(chartOfAccounts, officeId, office?.DepartureFeeCcId, costCodeById, accountingOffice);
    }

    private int GetDefaultPetAccount(List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        return GetDefaultOfficeExpenseAccount(chartOfAccounts, officeId, office?.PetFeeCcId, costCodeById, accountingOffice);
    }

    // Tenant/Owner/Company Accounts
    private int GetDefaultTenantIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultTenantIncome), chartOfAccounts, officeId, costCode, () =>
        {
            int defaultAccountId;
            if (accountingOffice?.DefaultTenantIncAccountId is > 0)
                defaultAccountId = accountingOffice.DefaultTenantIncAccountId.Value;
            else
            {
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

                defaultAccountId = account.AccountId;
            }

            return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
        });
    }

    private int GetDefaultTenantExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultTenantExpense), chartOfAccounts, officeId, costCode, () =>
        {
            int defaultAccountId;
            if (accountingOffice?.DefaultTenantExpAccountId is > 0)
                defaultAccountId = accountingOffice.DefaultTenantExpAccountId.Value;
            else
            {
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

                defaultAccountId = account.AccountId;
            }

            return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
        });
    }

    private int GetDefaultOwnerIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultOwnerIncome), chartOfAccounts, officeId, costCode, () =>
        {
            int defaultAccountId;
            if (accountingOffice?.DefaultOwnerIncAccountId is > 0)
                defaultAccountId = accountingOffice.DefaultOwnerIncAccountId.Value;
            else
            {
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

                defaultAccountId = account.AccountId;
            }

            return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
        });
    }

    private int GetDefaultOwnerExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultOwnerExpense), chartOfAccounts, officeId, costCode, () =>
        {
            int defaultAccountId;
            if (accountingOffice?.DefaultOwnerExpAccountId is > 0)
                defaultAccountId = accountingOffice.DefaultOwnerExpAccountId.Value;
            else
            {
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

                defaultAccountId = account.AccountId;
            }

            return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
        });
    }

    private int GetDefaultCompanyExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultCompanyExpense), chartOfAccounts, officeId, costCode, () =>
        {
            int defaultAccountId;
            if (accountingOffice?.DefaultCompanyExpAccountId is > 0)
                defaultAccountId = accountingOffice.DefaultCompanyExpAccountId.Value;
            else
            {
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

                defaultAccountId = account.AccountId;
            }

            return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
        });
    }

    private int GetDefaultPmUtilityIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultPmUtilityIncome), chartOfAccounts, officeId, costCode, () =>
        {
            int defaultAccountId;
            if (accountingOffice?.DefaultPmUtilityIncAccountId is > 0)
                defaultAccountId = accountingOffice.DefaultPmUtilityIncAccountId.Value;
            else
            {
                var account = chartOfAccounts
                    .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                    .Where(a =>
                        a.Name.Contains("PM Utility", StringComparison.OrdinalIgnoreCase) ||
                        a.Name.Contains("Utility", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(a => a.AccountId)
                    .FirstOrDefault()
                    ?? chartOfAccounts
                        .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                        .OrderBy(a => a.AccountId)
                        .FirstOrDefault();

                if (account == null)
                    throw new Exception($"No PM Utility Income chart of account is configured for office {officeId}");

                defaultAccountId = account.AccountId;
            }

            return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
        });
    }

    private int GetDefaultLaborIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        int defaultAccountId;
        if (accountingOffice?.@DefaultLaborIncAccountId is > 0)
            defaultAccountId = accountingOffice.@DefaultLaborIncAccountId.Value;
        else
        {
            var account = chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                .Where(a => a.Name.Contains("PM Utility", StringComparison.OrdinalIgnoreCase) )
                .OrderBy(a => a.AccountId)
                .FirstOrDefault()
                ?? chartOfAccounts
                    .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                    .OrderBy(a => a.AccountId)
                    .FirstOrDefault();

            if (account == null)
                throw new Exception($"No PM Utility Income chart of account is configured for office {officeId}");

            defaultAccountId = account.AccountId;
        }

        return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
    }

    private int GetDefaultLinenAndTowelIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        int defaultAccountId;
        if (accountingOffice?.@DefaultLinenTowelIncAccountId is > 0)
            defaultAccountId = accountingOffice.@DefaultLinenTowelIncAccountId.Value;
        else
        {
            var account = chartOfAccounts
                .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                .Where(a =>
                    a.Name.Contains("Linen", StringComparison.OrdinalIgnoreCase) ||
                    a.Name.Contains("Towel", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.AccountId)
                .FirstOrDefault()
                ?? chartOfAccounts
                    .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
                    .OrderBy(a => a.AccountId)
                    .FirstOrDefault();

            if (account == null)
                throw new Exception($"No PM Utility Income chart of account is configured for office {officeId}");

            defaultAccountId = account.AccountId;
        }

        return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
    }

    private int GetDefaultDepartureIncome(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        int defaultAccountId;
        if (accountingOffice?.DefaultDepartureIncAccountId is > 0)
            defaultAccountId = accountingOffice.DefaultDepartureIncAccountId.Value;
        else
        {
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

            defaultAccountId = account.AccountId;
        }

        return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
    }

    private int GetDefaultDepartureExpense(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, CostCode? costCode = null)
    {
        int defaultAccountId;
        if (accountingOffice?.DefaultDepartureExpAccountId is > 0)
            defaultAccountId = accountingOffice.DefaultDepartureExpAccountId.Value;
        else
        {
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

            defaultAccountId = account.AccountId;
        }

        return GetChartOfAccountIdByCostCode(chartOfAccounts, officeId, costCode, defaultAccountId);
    }

    // Bank & Balance Sheet Accounts
    private int GetDefaultBankAccount(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultBankAccount), chartOfAccounts, officeId, null, () =>
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
        });
    }

    private int GetDefaultAccountsReceivable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultAccountsReceivable), chartOfAccounts, officeId, null, () =>
        {
            if (accountingOffice?.DefaultActRcvableAccountId is > 0)
                return accountingOffice.DefaultActRcvableAccountId.Value;

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
        });
    }

    private int GetDefaultDepositAccount(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultDepositAccount), chartOfAccounts, officeId, null, () =>
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
        });
    }

    private int GetDefaultUndepositedFunds(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultUndepositedFunds), chartOfAccounts, officeId, null, () =>
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
        });
    }

    private int GetDefaultAccountsPayable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultAccountsPayable), chartOfAccounts, officeId, null, () =>
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
        });
    }

    private int GetDefaultOwnerAccountsPayable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultOwnerAccountsPayable), chartOfAccounts, officeId, null, () =>
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
        });
    }

    private int GetDefaultPrePayment(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        return ResolveDefaultAccountIdCached(nameof(GetDefaultPrePayment), chartOfAccounts, officeId, null, () =>
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
        });
    }
    #endregion

    #region Journal Entry Account Resolution
    private int ResolveDefaultAccountIdCached(string resolverKey, List<ChartOfAccount> chartOfAccounts, int officeId, CostCode? costCode, Func<int> resolver)
    {
        var key = new AccountResolverCacheKey(
            ChartContextId: RuntimeHelpers.GetHashCode(chartOfAccounts),
            OfficeId: officeId,
            ResolverKey: resolverKey,
            CostCodeKey: NormalizeAccountCode(costCode?.Code ?? string.Empty));
        return _defaultAccountIdCache.GetOrAdd(key, _ => resolver());
    }

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

    private int GetBillReceiptExpenseAccountId(ReceiptSplit split, int officeId, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
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
            ReceiptType.Company => GetDefaultCompanyExpense(chartOfAccounts, officeId, accountingOffice),
            _ => 0 // NonExpense
        };
    }

    private int GetBillReceiptLiabilityAccountId(Receipt bill, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        if (bill.BankCardId is > 0)
            return GetCreditCardAccountId(bill, chartOfAccounts, accountingOffice);

        return GetDefaultAccountsPayable(chartOfAccounts, bill.OfficeId, accountingOffice);
    }
    #endregion

    #region Accounting Log Logging
    private async Task LogAccountingLogAsync(AccountingLog log)
    {
        await _accountingRepository.LogAccountingLogAsync(log);
    }
    #endregion

    #region Accounting Error Logging
    private async Task LogAccountingErrorAsync(string trigger, Guid organizationId, int? officeId, int? sourceTypeId, Guid? sourceId, string? documentCode, DateOnly? accountingPeriod, decimal? amount, string message, Guid currentUser)
    {
        await _accountingRepository.LogAccountingErrorAsync(new AccountingError
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            Trigger = trigger,
            SourceTypeId = sourceTypeId,
            SourceId = sourceId,
            DocumentCode = documentCode,
            AccountingPeriod = accountingPeriod,
            Amount = amount,
            Message = message,
            CreatedBy = currentUser
        });
    }
    #endregion

    #region Journal Entry Date Helpers
    private static DateOnly ResolveBillOrReceiptJournalEntryDate(Receipt billOrReceipt)
    {
        if (billOrReceipt.ReceiptDate == default)
            throw new Exception("ReceiptDate is required to create a journal entry for a bill or receipt");
        return billOrReceipt.ReceiptDate;
    }

    private static DateOnly ResolveInvoicePaymentJournalEntryDate(LedgerLine paymentLedgerLine)
    {
        if (paymentLedgerLine.LedgerLineDate == default)
            throw new Exception("Payment date is required to create an invoice payment journal entry");
        return paymentLedgerLine.LedgerLineDate;
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

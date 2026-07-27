using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Recap Rent/4000 and owner-share rent base: invoice ledger lines whose cost code maps to the
    /// default tenant income account (4000) or its subaccounts.
    /// </summary>
    private async Task<decimal> GetInvoiceRentPlus4000BaseAsync(Invoice invoice)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode != null
                    && !IsPaymentLedgerLine(costCode)
                    && IsRentalIncomeCostCode(costCode, chartOfAccounts, invoice.OfficeId, accountingOffice);
            })
            .Sum(line => line.Amount);
    }

    /// <summary>
    /// Rent-only ledger base for owner expected and owner actual share. Uses the default tenant
    /// income (4000) account tree and excludes fees, security deposit, and SDW even when COA mapping
    /// would otherwise treat a fee line as rental income.
    /// </summary>
    private async Task<decimal> GetInvoiceOwnerRentShareBaseAsync(Invoice invoice)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var (office, _) = await LoadOfficeCostCodeContextAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                if (!costCodeById.TryGetValue(line.CostCodeId, out var costCode))
                    return false;

                if (IsOwnerShareNonRentChargeCostCode(costCode, office))
                    return false;

                return IsRentalIncomeCostCode(costCode, chartOfAccounts, invoice.OfficeId, accountingOffice);
            })
            .Sum(line => line.Amount);
    }

    private async Task<decimal> GetInvoiceSecurityDepositBaseAsync(Invoice invoice)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode?.TransactionType == TransactionType.SecurityDeposit;
            })
            .Sum(line => line.Amount);
    }

    private async Task<decimal> GetInvoiceSecurityDepositWaiverBaseAsync(Invoice invoice)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode?.TransactionType == TransactionType.SecurityDepositWaiver;
            })
            .Sum(line => line.Amount);
    }

    private async Task<decimal> GetInvoiceFeesBaseAsync(Invoice invoice)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                if (costCode == null || IsPaymentLedgerLine(costCode))
                    return false;

                if (IsRentalIncomeCostCode(costCode, chartOfAccounts, invoice.OfficeId, accountingOffice))
                    return false;

                if (costCode.TransactionType is TransactionType.SecurityDeposit or TransactionType.SecurityDepositWaiver)
                    return false;

                return true;
            })
            .Sum(line => line.Amount);
    }

    /// <summary>
    /// Recap Rent/4000 uses the same rental-income account tree as <see cref="GetInvoiceRentPlus4000BaseAsync"/>.
    /// </summary>
    public static bool IsRentPlus4000JournalCreditLine(int? sourceTypeId, decimal credit, bool isRentalIncomeAccount)
        => sourceTypeId == (int)SourceType.Invoice && credit > 0 && isRentalIncomeAccount;

    private static bool IsRentPlus4000CostCode(CostCode costCode, IReadOnlyList<ChartOfAccount> chartOfAccounts, int officeId)
        => IsRentalIncomeCostCode(costCode, chartOfAccounts, officeId, accountingOffice: null);

    private static bool IsRentalIncomeCostCode(
        CostCode costCode,
        IReadOnlyList<ChartOfAccount> chartOfAccounts,
        int officeId,
        AccountingOffice? accountingOffice)
    {
        var normalized = NormalizeAccountCode(costCode.Code);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.OfficeId == officeId &&
            NormalizeAccountCode(a.AccountNo).Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (account == null)
            return false;

        var rentalIncomeRootAccountNo = ResolveRentalIncomeRootAccountNo(chartOfAccounts, officeId, accountingOffice);
        if (string.IsNullOrWhiteSpace(rentalIncomeRootAccountNo))
            return false;

        return IsAccountInRentalIncomeTree(account, chartOfAccounts, officeId, rentalIncomeRootAccountNo);
    }

    private static string? ResolveRentalIncomeRootAccountNo(
        IReadOnlyList<ChartOfAccount> chartOfAccounts,
        int officeId,
        AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultTenantIncAccountId is > 0)
        {
            var rootAccount = chartOfAccounts.FirstOrDefault(a =>
                a.OfficeId == officeId
                && a.AccountId == accountingOffice.DefaultTenantIncAccountId.Value);
            var normalized = NormalizeAccountCode(rootAccount?.AccountNo);
            if (!string.IsNullOrWhiteSpace(normalized))
                return normalized;
        }

        return "4000";
    }

    private static bool IsAccountInRentalIncomeTree(
        ChartOfAccount account,
        IReadOnlyList<ChartOfAccount> chartOfAccounts,
        int officeId,
        string rentalIncomeRootAccountNo)
    {
        var visitedAccountIds = new HashSet<int>();
        var current = account;
        while (current != null && visitedAccountIds.Add(current.AccountId))
        {
            if (NormalizeAccountCode(current.AccountNo ?? string.Empty).Equals(rentalIncomeRootAccountNo, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!current.IsSubaccount || !current.SubAccountId.HasValue)
                return false;

            current = chartOfAccounts.FirstOrDefault(a =>
                a.OfficeId == officeId
                && a.AccountId == current.SubAccountId.Value);
        }

        return false;
    }

    private static bool IsOwnerShareNonRentChargeCostCode(CostCode costCode, Office? office)
    {
        if (IsPaymentLedgerLine(costCode))
            return true;

        if (costCode.TransactionType is TransactionType.SecurityDeposit or TransactionType.SecurityDepositWaiver)
            return true;

        if (office == null)
            return false;

        if (office.DepartureFeeCcId is > 0 && costCode.CostCodeId == office.DepartureFeeCcId)
            return true;

        if (office.PetFeeCcId is > 0 && costCode.CostCodeId == office.PetFeeCcId)
            return true;

        return false;
    }
}

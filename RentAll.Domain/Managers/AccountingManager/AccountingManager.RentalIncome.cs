using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Rent/4000 and owner-share rent base: sum invoice lines whose cost code maps to an explicit
    /// chart-of-account in the office rental income tree from GetRentalIncomeAccounts.
    /// </summary>
    private async Task<decimal> GetInvoiceRentPlus4000BaseAsync(Invoice invoice)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var (office, _) = await LoadOfficeCostCodeContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var rentalIncomeAccountIds = GetRentalIncomeAccounts(chartOfAccounts, invoice.OfficeId, office, costCodeById, accountingOffice)
            .Select(account => account.AccountId)
            .ToHashSet();

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return IsInvoiceLineRentalIncome(costCode, chartOfAccounts, invoice.OfficeId, rentalIncomeAccountIds);
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
        var (office, _) = await LoadOfficeCostCodeContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var rentalIncomeAccountIds = GetRentalIncomeAccounts(chartOfAccounts, invoice.OfficeId, office, costCodeById, accountingOffice)
            .Select(account => account.AccountId)
            .ToHashSet();

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                if (costCode == null || IsPaymentLedgerLine(costCode))
                    return false;

                if (costCode.TransactionType is TransactionType.SecurityDeposit or TransactionType.SecurityDepositWaiver)
                    return false;

                return !IsInvoiceLineRentalIncome(costCode, chartOfAccounts, invoice.OfficeId, rentalIncomeAccountIds);
            })
            .Sum(line => line.Amount);
    }

    public static bool IsRentPlus4000JournalCreditLine(int? sourceTypeId, decimal credit, bool isRentalIncomeAccount)
        => sourceTypeId == (int)SourceType.Invoice && credit > 0 && isRentalIncomeAccount;

    private static bool IsCostCodeInRentalIncomeTree(CostCode costCode, List<ChartOfAccount> chartOfAccounts, int officeId, Office? office, IReadOnlyDictionary<int, CostCode> costCodeById, AccountingOffice? accountingOffice)
    {
        var rentalIncomeAccountIds = GetRentalIncomeAccounts(chartOfAccounts, officeId, office, costCodeById, accountingOffice)
            .Select(account => account.AccountId)
            .ToHashSet();
        return IsCostCodeMappedToRentalIncomeAccount(costCode, chartOfAccounts, officeId, rentalIncomeAccountIds);
    }

    private static bool IsInvoiceLineRentalIncome(CostCode? costCode, List<ChartOfAccount> chartOfAccounts, int officeId, IReadOnlySet<int> rentalIncomeAccountIds)
    {
        if (costCode == null || IsPaymentLedgerLine(costCode))
            return false;

        if (costCode.TransactionType is TransactionType.SecurityDeposit or TransactionType.SecurityDepositWaiver)
            return false;

        return IsCostCodeMappedToRentalIncomeAccount(costCode, chartOfAccounts, officeId, rentalIncomeAccountIds);
    }

    private static bool IsCostCodeMappedToRentalIncomeAccount(CostCode costCode, List<ChartOfAccount> chartOfAccounts, int officeId, IReadOnlySet<int> rentalIncomeAccountIds)
    {
        var accountCode = NormalizeAccountCode(costCode.Code);
        if (string.IsNullOrWhiteSpace(accountCode))
            return false;

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.OfficeId == officeId
            && NormalizeAccountCode(a.AccountNo).Equals(accountCode, StringComparison.OrdinalIgnoreCase));
        if (account == null)
            return false;

        return rentalIncomeAccountIds.Contains(account.AccountId);
    }
}

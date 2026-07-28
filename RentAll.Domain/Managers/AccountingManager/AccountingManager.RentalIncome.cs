using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Single source for Rent/4000 and the owner expected rent percentage base:
    /// invoice ledger lines whose cost code maps to account 4000 or its subaccounts.
    /// </summary>
    private async Task<decimal> GetInvoiceRentPlus4000BaseAsync(Invoice invoice)
    {
        var (chartOfAccounts, _) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode != null
                    && !IsPaymentLedgerLine(costCode)
                    && IsRentPlus4000CostCode(costCode, chartOfAccounts, invoice.OfficeId);
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
        var (chartOfAccounts, _) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);

        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                if (costCode == null || IsPaymentLedgerLine(costCode))
                    return false;

                if (IsRentPlus4000CostCode(costCode, chartOfAccounts, invoice.OfficeId))
                    return false;

                if (costCode.TransactionType is TransactionType.SecurityDeposit or TransactionType.SecurityDepositWaiver)
                    return false;

                return true;
            })
            .Sum(line => line.Amount);
    }

    /// <summary>
    /// Recap Rent/4000 uses the same 4000 rental-income account tree as <see cref="GetInvoiceRentPlus4000BaseAsync"/>.
    /// </summary>
    public static bool IsRentPlus4000JournalCreditLine(int? sourceTypeId, decimal credit, bool isRentalIncomeAccount)
        => sourceTypeId == (int)SourceType.Invoice && credit > 0 && isRentalIncomeAccount;

    private static bool IsRentPlus4000CostCode(CostCode costCode, IReadOnlyList<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var normalized = NormalizeAccountCode(costCode.Code);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.OfficeId == officeId &&
            NormalizeAccountCode(a.AccountNo).Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (account == null)
            return false;

        var visitedAccountIds = new HashSet<int>();
        var current = account;
        while (current != null && visitedAccountIds.Add(current.AccountId))
        {
            if (NormalizeAccountCode(current.AccountNo).Equals("4000", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!current.IsSubaccount || !current.SubAccountId.HasValue)
                return false;

            current = chartOfAccounts.FirstOrDefault(a =>
                a.OfficeId == officeId &&
                a.AccountId == current.SubAccountId.Value);
        }

        return false;
    }
}

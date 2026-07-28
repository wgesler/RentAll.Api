using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private sealed record InvoiceChargeBases(decimal RentalIncome, decimal SecurityDeposit, decimal SecurityDepositWaiver, decimal Fees)
    {
        public decimal ChargeTotal => RentalIncome + SecurityDeposit + SecurityDepositWaiver + Fees;
        public decimal NonRentTotal => SecurityDeposit + SecurityDepositWaiver + Fees;
    }

    private sealed class InvoiceRentalIncomeContext
    {
        public required int OfficeId { get; init; }
        public required List<ChartOfAccount> ChartOfAccounts { get; init; }
        public required IReadOnlyDictionary<int, CostCode> CostCodeById { get; init; }
        public required HashSet<int> RentalIncomeAccountIds { get; init; }
    }

    /// <summary>
    /// Classifies non-payment invoice charge lines into rental income, SD, SDW, and fees using the
    /// office rental income account tree resolved from FurnishedRentChargeCcId.
    /// </summary>
    private async Task<InvoiceChargeBases> GetInvoiceChargeBasesAsync(Invoice invoice)
    {
        var context = await LoadInvoiceRentalIncomeContextAsync(invoice);
        decimal rentalIncome = 0m;
        decimal securityDeposit = 0m;
        decimal securityDepositWaiver = 0m;
        decimal fees = 0m;

        foreach (var line in invoice.LedgerLines.Where(line => line.Amount != 0))
        {
            context.CostCodeById.TryGetValue(line.CostCodeId, out var costCode);
            if (costCode == null || IsPaymentLedgerLine(costCode))
                continue;

            if (costCode.TransactionType == TransactionType.SecurityDeposit)
            {
                securityDeposit += line.Amount;
                continue;
            }

            if (costCode.TransactionType == TransactionType.SecurityDepositWaiver)
            {
                securityDepositWaiver += line.Amount;
                continue;
            }

            if (IsRentalIncomeInvoiceLine(costCode, context))
                rentalIncome += line.Amount;
            else
                fees += line.Amount;
        }

        return new InvoiceChargeBases(rentalIncome, securityDeposit, securityDepositWaiver, fees);
    }

    public static bool IsRentPlus4000JournalCreditLine(int? sourceTypeId, decimal credit, bool isRentalIncomeAccount)
        => sourceTypeId == (int)SourceType.Invoice && credit > 0 && isRentalIncomeAccount;

    private async Task<InvoiceRentalIncomeContext> LoadInvoiceRentalIncomeContextAsync(Invoice invoice)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var (office, _) = await LoadOfficeCostCodeContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var rentalIncomeAccountIds = GetRentalIncomeAccounts(chartOfAccounts, invoice.OfficeId, office, costCodeById, accountingOffice)
            .Select(account => account.AccountId)
            .ToHashSet();

        return new InvoiceRentalIncomeContext
        {
            OfficeId = invoice.OfficeId,
            ChartOfAccounts = chartOfAccounts,
            CostCodeById = costCodeById,
            RentalIncomeAccountIds = rentalIncomeAccountIds
        };
    }

    private static bool IsRentalIncomeInvoiceLine(CostCode costCode, InvoiceRentalIncomeContext context)
    {
        if (IsPaymentLedgerLine(costCode))
            return false;

        if (costCode.TransactionType is TransactionType.SecurityDeposit or TransactionType.SecurityDepositWaiver)
            return false;

        return IsCostCodeMappedToRentalIncomeAccount(costCode, context.ChartOfAccounts, context.OfficeId, context.RentalIncomeAccountIds);
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

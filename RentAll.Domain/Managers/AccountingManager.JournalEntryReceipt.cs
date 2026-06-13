using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task<JournalEntry> CreateJournalEntryFromReceiptAsync(Receipt receipt, Guid currentUser)
    {
        if (receipt.ReceiptId <= 0)
            throw new Exception("ReceiptId is required to create a receipt journal entry");

        EnsureReceiptIsCardReceipt(receipt);

        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = receipt.OrganizationId,
            OfficeIds = receipt.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Receipt,
            SourceReceiptId = receipt.ReceiptId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var journalEntry = await BuildJournalEntryFromReceiptAsync(receipt, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }
    #endregion

    #region Journal Entry
    async Task<JournalEntry> BuildJournalEntryFromReceiptAsync(Receipt receipt, Guid currentUser)
    {
        EnsureReceiptIsCardReceipt(receipt);

        var splitLines = receipt.Splits
            .Where(s => s.Amount != 0)
            .OrderBy(s => s.ReceiptSplitId)
            .ToList();

        if (splitLines.Count == 0)
            throw new Exception("Receipt has no split lines to create a journal entry");

        var totalAmount = splitLines.Sum(s => s.Amount);
        if (totalAmount == 0)
            throw new Exception("Receipt total is zero");

        if (receipt.AccountingPeriod == default)
            throw new Exception("AccountingPeriod is required to create a journal entry for a receipt");

        var chartOfAccounts = await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(receipt.OrganizationId, receipt.OfficeId);
        var creditCardAccountId = await ResolveCreditCardAccountIdAsync(receipt, chartOfAccounts);
        var defaultExpenseAccountId = ResolveDefaultExpenseAccountId(chartOfAccounts, receipt.OfficeId);
        var defaultCostOfGoodsSoldAccountId = ResolveDefaultCostOfGoodsSoldAccountId(chartOfAccounts, receipt.OfficeId);

        var transactionDate = receipt.ReceiptDate != default ? receipt.ReceiptDate : receipt.AccountingPeriod;
        var postingDate = receipt.AccountingPeriod;
        var receiptLabel = receipt.ReceiptId.ToString();
        var memo = string.IsNullOrWhiteSpace(receipt.Description)
            ? $"Receipt {receiptLabel}"
            : receipt.Description.Trim();
        var propertyId = receipt.PropertyIds.FirstOrDefault(id => id != Guid.Empty);

        var journalEntryLines = new List<JournalEntryLine>
        {
            new()
            {
                ChartOfAccountId = creditCardAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = receipt.VendorId,
                Debit = 0,
                Credit = totalAmount,
                Memo = $"Credit Card - {receipt.BankCardDisplayName}".Trim(),
                CreatedBy = currentUser
            }
        };

        foreach (var split in splitLines)
        {
            var expenseAccountId = ResolveExpenseOrCogsAccountId(
                split,
                chartOfAccounts,
                receipt.OfficeId,
                defaultCostOfGoodsSoldAccountId,
                defaultExpenseAccountId);

            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = expenseAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = receipt.VendorId,
                Debit = split.Amount,
                Credit = 0,
                Memo = split.Description,
                CreatedBy = currentUser
            });
        }

        return new JournalEntry
        {
            OrganizationId = receipt.OrganizationId,
            OfficeId = receipt.OfficeId,
            TransactionDate = transactionDate,
            PostingDate = postingDate,
            SourceTypeId = (int)SourceType.Receipt,
            SourceReceiptId = receipt.ReceiptId,
            Memo = memo,
            JournalEntryLines = journalEntryLines,
            CreatedBy = currentUser
        };
    }
    #endregion

    #region Static Helpers
    static void EnsureReceiptIsCardReceipt(Receipt receipt)
    {
        if (receipt.BankCardId is not > 0)
            throw new Exception("Receipt is not a card receipt");
    }

    async Task<int> ResolveCreditCardAccountIdAsync(Receipt receipt, List<ChartOfAccount> chartOfAccounts)
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
        var defaultCreditCardAccountId = ResolveDefaultCreditCardAccountId(chartOfAccounts, receipt.OfficeId);
        return ResolveChartOfAccountIdForCostCode(costCode, chartOfAccounts, receipt.OfficeId, defaultCreditCardAccountId);
    }

    static int ResolveDefaultCreditCardAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.CreditCard)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Credit Card chart of account is configured for office {officeId}");

        return account.AccountId;
    }
    #endregion
}

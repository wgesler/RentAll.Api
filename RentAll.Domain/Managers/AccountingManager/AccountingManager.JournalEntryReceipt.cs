using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task<JournalEntry> CreateJournalEntryFromReceiptAsync(Receipt receipt, Guid currentUser)
    {
        EnsureReceiptIsCardReceipt(receipt);

        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = receipt.OrganizationId,
            OfficeIds = receipt.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Receipt,
            SourceId = receipt.ReceiptGuid,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(receipt.OrganizationId, receipt.OfficeId);
        var journalEntry = await BuildJournalEntryFromReceiptAsync(receipt, chartOfAccounts, accountingOffice, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }
    #endregion

    #region Journal Entry
    async Task<JournalEntry> BuildJournalEntryFromReceiptAsync(Receipt receipt, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser)
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

        var creditCardAccountId = await GetCreditCardAccountIdAsync(receipt, chartOfAccounts, accountingOffice);
        var defaultExpenseAccountId = GetCompanyExpenseAccountId(chartOfAccounts, receipt.OfficeId, accountingOffice);
        var defaultCostOfGoodsSoldAccountId = GetCostOfGoodsSoldAccountIdByNameOrType(chartOfAccounts, receipt.OfficeId);

        var transactionDate = receipt.ReceiptDate != default ? receipt.ReceiptDate : receipt.AccountingPeriod;
        var postingDate = receipt.AccountingPeriod;
        var receiptLabel = receipt.ReceiptCode.Trim();
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
            var expenseAccountId = GetExpenseOrCogsAccountId(
                chartOfAccounts,
                receipt.OfficeId,
                split,
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
            SourceId = receipt.ReceiptGuid,
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

    #endregion
}

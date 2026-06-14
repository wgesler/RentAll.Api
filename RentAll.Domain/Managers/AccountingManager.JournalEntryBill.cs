using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task<JournalEntry> CreateJournalEntryFromBillAsync(Receipt bill, Guid currentUser)
    {
        EnsureReceiptIsBill(bill);

        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = bill.OrganizationId,
            OfficeIds = bill.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Bill,
            SourceId = bill.ReceiptGuid,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var journalEntry = await BuildJournalEntryFromBillAsync(bill, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    public async Task<List<JournalEntry>> CreateJournalEntriesFromBillPaymentAsync(BillPayment billPayment, Guid currentUser)
    {
        var journalEntries = new List<JournalEntry>();

        foreach (var paymentApplication in billPayment.PaymentApplications)
        {
            var journalEntry = await CreateJournalEntryFromBillPaymentAsync(paymentApplication, currentUser);
            journalEntries.Add(journalEntry);
        }

        return journalEntries;
    }
    #endregion

    #region Journal Entry
    async Task<JournalEntry> BuildJournalEntryFromBillAsync(Receipt bill, Guid currentUser)
    {
        EnsureReceiptIsBill(bill);

        var splitLines = bill.Splits
            .Where(s => s.Amount != 0)
            .OrderBy(s => s.ReceiptSplitId)
            .ToList();

        if (splitLines.Count == 0)
            throw new Exception("Bill has no split lines to create a journal entry");

        var totalAmount = splitLines.Sum(s => s.Amount);
        if (totalAmount == 0)
            throw new Exception("Bill total is zero");

        if (bill.AccountingPeriod == default)
            throw new Exception("AccountingPeriod is required to create a journal entry for a bill");

        var chartOfAccounts = await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(bill.OrganizationId, bill.OfficeId);
        var accountsPayableAccountId = ResolveAccountsPayableAccountId(chartOfAccounts, bill.OfficeId);
        var accountsReceivableAccountId = ResolveAccountsReceivableAccountId(chartOfAccounts, bill.OfficeId);
        var undepositedFundsAccountId = ResolveUndepositedFundsAccountId(chartOfAccounts, bill.OfficeId);
        var defaultExpenseAccountId = ResolveDefaultExpenseAccountId(chartOfAccounts, bill.OfficeId);
        var defaultCostOfGoodsSoldAccountId = chartOfAccounts
            .Where(a => a.OfficeId == bill.OfficeId && a.AccountType == AccountType.CostOfGoodsSold)
            .OrderBy(a => a.AccountId)
            .Select(a => a.AccountId)
            .FirstOrDefault();

        var transactionDate = bill.ReceiptDate != default ? bill.ReceiptDate : bill.AccountingPeriod;
        var postingDate = bill.AccountingPeriod;
        var billLabel = !string.IsNullOrWhiteSpace(bill.BillNumber)
            ? bill.BillNumber.Trim()
            : bill.ReceiptCode.Trim();
        var memo = string.IsNullOrWhiteSpace(bill.Description)
            ? $"Bill {billLabel}"
            : bill.Description.Trim();
        var propertyId = bill.PropertyIds.FirstOrDefault(id => id != Guid.Empty);

        var positiveSplits = splitLines.Where(s => s.Amount > 0).ToList();
        var negativeSplits = splitLines.Where(s => s.Amount < 0).ToList();
        var journalEntryLines = new List<JournalEntryLine>();

        var positiveTotal = positiveSplits.Sum(s => s.Amount);
        if (positiveTotal > 0)
        {
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = accountsPayableAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = 0,
                Credit = positiveTotal,
                Memo = $"Accounts Payable - {billLabel}",
                CreatedBy = currentUser
            });

            foreach (var split in positiveSplits)
            {
                var expenseAccountId = ResolveExpenseOrCogsAccountId(
                    split,
                    chartOfAccounts,
                    bill.OfficeId,
                    defaultCostOfGoodsSoldAccountId,
                    defaultExpenseAccountId);

                journalEntryLines.Add(new JournalEntryLine
                {
                    ChartOfAccountId = expenseAccountId,
                    PropertyId = propertyId == Guid.Empty ? null : propertyId,
                    ContactId = bill.VendorId,
                    Debit = split.Amount,
                    Credit = 0,
                    Memo = split.Description,
                    CreatedBy = currentUser
                });
            }
        }

        foreach (var split in negativeSplits)
        {
            var creditAmount = Math.Abs(split.Amount);
            var billCreditMemo = string.IsNullOrWhiteSpace(split.Description)
                ? $"Bill Credit - {billLabel}"
                : split.Description.Trim();

            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = undepositedFundsAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = creditAmount,
                Credit = 0,
                Memo = billCreditMemo,
                CreatedBy = currentUser
            });

            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = accountsReceivableAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = 0,
                Credit = creditAmount,
                Memo = billCreditMemo,
                CreatedBy = currentUser
            });
        }

        return new JournalEntry
        {
            OrganizationId = bill.OrganizationId,
            OfficeId = bill.OfficeId,
            TransactionDate = transactionDate,
            PostingDate = postingDate,
            SourceTypeId = (int)SourceType.Bill,
            SourceId = bill.ReceiptGuid,
            Memo = memo,
            JournalEntryLines = journalEntryLines,
            CreatedBy = currentUser
        };
    }

    async Task<JournalEntry> CreateJournalEntryFromBillPaymentAsync(BillPaymentApplication paymentApplication, Guid currentUser)
    {
        if (paymentApplication.PaymentSequence < 0)
            throw new Exception("PaymentSequence is required to create a bill payment journal entry");

        var bill = paymentApplication.Bill;
        if (bill == null)
            throw new Exception("Bill is required to create a bill payment journal entry");

        var sourceId = CreateBillPaymentSourceId(bill.ReceiptGuid, paymentApplication.PaymentSequence);
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = bill.OrganizationId,
            OfficeIds = bill.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.BillPayment,
            SourceId = sourceId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var journalEntry = await BuildJournalEntryFromBillPaymentAsync(paymentApplication, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    async Task<JournalEntry> BuildJournalEntryFromBillPaymentAsync(BillPaymentApplication paymentApplication, Guid currentUser)
    {
        var bill = paymentApplication.Bill;
        if (paymentApplication.AmountApplied == 0)
            throw new Exception("Bill payment amount cannot be zero");

        if (paymentApplication.PaymentDate == default)
            throw new Exception("Payment date is required to create a bill payment journal entry");

        var chartOfAccounts = await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(bill.OrganizationId, bill.OfficeId);
        var liabilityAccountId = await ResolveBillLiabilityAccountIdAsync(bill, chartOfAccounts);
        var offsetAccountId = ResolveBillPaymentChartOfAccountId(
            paymentApplication.ChartOfAccountId,
            chartOfAccounts,
            bill.OfficeId);

        var amount = Math.Abs(paymentApplication.AmountApplied);
        var transactionDate = paymentApplication.PaymentDate;
        var postingDate = paymentApplication.PaymentDate;
        var billLabel = !string.IsNullOrWhiteSpace(bill.BillNumber)
            ? bill.BillNumber.Trim()
            : bill.ReceiptCode.Trim();
        var memo = string.IsNullOrWhiteSpace(paymentApplication.Description)
            ? $"Bill Payment - {billLabel}"
            : paymentApplication.Description.Trim();
        var propertyId = bill.PropertyIds.FirstOrDefault(id => id != Guid.Empty);
        var liabilityMemo = bill.BankCardId is > 0
            ? $"Credit Card - {bill.BankCardDisplayName}".Trim()
            : $"Accounts Payable - {billLabel}";

        JournalEntryLine offsetLine;
        JournalEntryLine liabilityLine;

        if (paymentApplication.AmountApplied > 0)
        {
            liabilityLine = new JournalEntryLine
            {
                ChartOfAccountId = liabilityAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = amount,
                Credit = 0,
                Memo = liabilityMemo,
                CreatedBy = currentUser
            };
            offsetLine = new JournalEntryLine
            {
                ChartOfAccountId = offsetAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = 0,
                Credit = amount,
                Memo = memo,
                CreatedBy = currentUser
            };
        }
        else
        {
            liabilityLine = new JournalEntryLine
            {
                ChartOfAccountId = liabilityAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = 0,
                Credit = amount,
                Memo = liabilityMemo,
                CreatedBy = currentUser
            };
            offsetLine = new JournalEntryLine
            {
                ChartOfAccountId = offsetAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = amount,
                Credit = 0,
                Memo = memo,
                CreatedBy = currentUser
            };
        }

        return new JournalEntry
        {
            OrganizationId = bill.OrganizationId,
            OfficeId = bill.OfficeId,
            TransactionDate = transactionDate,
            PostingDate = postingDate,
            SourceTypeId = (int)SourceType.BillPayment,
            SourceId = CreateBillPaymentSourceId(bill.ReceiptGuid, paymentApplication.PaymentSequence),
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { liabilityLine, offsetLine },
            CreatedBy = currentUser
        };
    }
    #endregion

    #region Static Helpers
    static int ResolveBillPaymentChartOfAccountId(int chartOfAccountId, List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        if (chartOfAccountId <= 0)
            throw new Exception("Chart of account is required for bill payment");

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.AccountId == chartOfAccountId && a.OfficeId == officeId);

        if (account == null)
            throw new Exception("Invalid chart of account for bill payment");

        return account.AccountId;
    }

    async Task<int> ResolveBillLiabilityAccountIdAsync(Receipt bill, List<ChartOfAccount> chartOfAccounts)
    {
        if (bill.BankCardId is > 0)
            return await ResolveCreditCardAccountIdAsync(bill, chartOfAccounts);

        return ResolveAccountsPayableAccountId(chartOfAccounts, bill.OfficeId);
    }

    static int ResolveExpenseOrCogsAccountId(
        ReceiptSplit split,
        List<ChartOfAccount> chartOfAccounts,
        int officeId,
        int defaultCostOfGoodsSoldAccountId,
        int defaultExpenseAccountId)
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

    async Task<int> GetNextBillPaymentSequenceAsync(Receipt bill)
    {
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = bill.OrganizationId,
            OfficeIds = bill.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.BillPayment,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        return existingEntries
            .Where(e => !e.IsVoided && e.SourceId is Guid sourceId)
            .Select(e => TryGetBillPaymentSequence(e.SourceId!.Value, bill.ReceiptGuid))
            .Where(sequence => sequence >= 0)
            .DefaultIfEmpty(-1)
            .Max() + 1;
    }

    static void EnsureReceiptIsBill(Receipt bill)
    {
        if (bill.BankCardId != null)
            throw new Exception("Receipt is not a bill");
    }

    static int ResolveAccountsPayableAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsPayable)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Accounts Payable chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    static int ResolveDefaultExpenseAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Expense)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Expense chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    static int ResolveDefaultCostOfGoodsSoldAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.CostOfGoodsSold)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Cost of Goods Sold chart of account is configured for office {officeId}");

        return account.AccountId;
    }
    #endregion
}

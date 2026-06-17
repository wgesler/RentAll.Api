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
            SourceId = bill.ReceiptId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(bill.OrganizationId, bill.OfficeId);
        var journalEntry = await BuildJournalEntryFromBillAsync(bill, chartOfAccounts, accountingOffice, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    public async Task<List<JournalEntry>> CreateJournalEntriesFromBillPaymentAsync(BillPayment billPayment, Guid currentUser)
    {
        var journalEntries = new List<JournalEntry>();

        if (billPayment.PaymentApplications.Count == 0
            || !await IsAccountingFeatureEnabledAsync(billPayment.PaymentApplications[0].Bill.OrganizationId))
            return journalEntries;

        foreach (var paymentApplication in billPayment.PaymentApplications)
        {
            var journalEntry = await CreateJournalEntryFromBillPaymentAsync(paymentApplication, currentUser);
            journalEntries.Add(journalEntry);
        }

        return journalEntries;
    }
    #endregion

    #region Journal Entry
    async Task<JournalEntry> BuildJournalEntryFromBillAsync(Receipt bill, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser)
    {
        EnsureReceiptIsBill(bill);

        var splitLines = ResolveDocumentSplitLines(bill);

        if (splitLines.Count == 0)
            throw new Exception("Bill has no split lines to create a journal entry");

        var totalAmount = splitLines.Sum(s => s.Amount);
        if (totalAmount == 0)
            throw new Exception("Bill total is zero");

        if (bill.AccountingPeriod == default)
            throw new Exception("AccountingPeriod is required to create a journal entry for a bill");

        var accountsPayableAccountId = GetAccountsPayableAccountId(chartOfAccounts, bill.OfficeId, accountingOffice);
        var accountsReceivableAccountId = GetAccountsReceivableAccountId(chartOfAccounts, bill.OfficeId, accountingOffice);
        var undepositedFundsAccountId = GetUndepositedFundsAccountId(chartOfAccounts, bill.OfficeId, accountingOffice);
        var defaultExpenseAccountId = GetCompanyExpenseAccountId(chartOfAccounts, bill.OfficeId, accountingOffice);
        var defaultCostOfGoodsSoldAccountId = GetCostOfGoodsSoldAccountIdByNameOrType(chartOfAccounts, bill.OfficeId);

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
            var (accountsPayableDebit, accountsPayableCredit) = SignedAmountToDebitCredit(positiveTotal, positiveIsDebit: false);
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = accountsPayableAccountId,
                PropertyId = propertyId == Guid.Empty ? null : propertyId,
                ContactId = bill.VendorId,
                Debit = accountsPayableDebit,
                Credit = accountsPayableCredit,
                Memo = $"Accounts Payable - {billLabel}",
                CreatedBy = currentUser
            });

            foreach (var split in positiveSplits)
            {
                var expenseAccountId = GetExpenseOrCogsAccountId(
                    chartOfAccounts,
                    bill.OfficeId,
                    split,
                    defaultCostOfGoodsSoldAccountId,
                    defaultExpenseAccountId);
                var (expenseDebit, expenseCredit) = SignedAmountToDebitCredit(split.Amount, positiveIsDebit: true);

                journalEntryLines.Add(new JournalEntryLine
                {
                    ChartOfAccountId = expenseAccountId,
                    PropertyId = propertyId == Guid.Empty ? null : propertyId,
                    ContactId = bill.VendorId,
                    Debit = expenseDebit,
                    Credit = expenseCredit,
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
            SourceId = bill.ReceiptId,
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

        if (bill.ReceiptId == Guid.Empty)
            throw new Exception("ReceiptId is required to create a bill payment journal entry");

        if (!await IsAccountingFeatureEnabledAsync(bill.OrganizationId))
            throw new Exception("Accounting is not enabled for this organization");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(bill.OrganizationId, bill.OfficeId);
        var journalEntry = await BuildJournalEntryFromBillPaymentAsync(paymentApplication, chartOfAccounts, accountingOffice, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    async Task<JournalEntry> BuildJournalEntryFromBillPaymentAsync(BillPaymentApplication paymentApplication, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser)
    {
        var bill = paymentApplication.Bill;
        if (paymentApplication.AmountApplied == 0)
            throw new Exception("Bill payment amount cannot be zero");

        if (paymentApplication.PaymentDate == default)
            throw new Exception("Payment date is required to create a bill payment journal entry");

        var liabilityAccountId = GetBillLiabilityAccountId(bill, chartOfAccounts, accountingOffice);
        var offsetAccountId = GetBillPaymentChartOfAccountId(
            chartOfAccounts,
            bill.OfficeId,
            paymentApplication.ChartOfAccountId);

        var amount = paymentApplication.AmountApplied;
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

        var (liabilityDebit, liabilityCredit) = SignedAmountToDebitCredit(amount, positiveIsDebit: true);
        var (offsetDebit, offsetCredit) = SignedAmountToDebitCredit(-amount, positiveIsDebit: true);

        var liabilityLine = new JournalEntryLine
        {
            ChartOfAccountId = liabilityAccountId,
            PropertyId = propertyId == Guid.Empty ? null : propertyId,
            ContactId = bill.VendorId,
            Debit = liabilityDebit,
            Credit = liabilityCredit,
            Memo = liabilityMemo,
            CreatedBy = currentUser
        };
        var offsetLine = new JournalEntryLine
        {
            ChartOfAccountId = offsetAccountId,
            PropertyId = propertyId == Guid.Empty ? null : propertyId,
            ContactId = bill.VendorId,
            Debit = offsetDebit,
            Credit = offsetCredit,
            Memo = memo,
            CreatedBy = currentUser
        };

        return new JournalEntry
        {
            OrganizationId = bill.OrganizationId,
            OfficeId = bill.OfficeId,
            TransactionDate = transactionDate,
            PostingDate = postingDate,
            SourceTypeId = (int)SourceType.BillPayment,
            SourceId = bill.ReceiptId,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { liabilityLine, offsetLine },
            CreatedBy = currentUser
        };
    }
    #endregion

    #region Static Helpers
    async Task<int> GetNextBillPaymentSequenceAsync(Receipt bill)
    {
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = bill.OrganizationId,
            OfficeIds = bill.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.BillPayment,
            SourceId = bill.ReceiptId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        return existingEntries.Count(e => !e.IsVoided);
    }

    static void EnsureReceiptIsBill(Receipt bill)
    {
        if (bill.BankCardId != null)
            throw new Exception("Receipt is not a bill");
    }
    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task<JournalEntry> CreateJournalEntryFromInvoiceAsync(Invoice invoice, Guid currentUser)
    {
        if (invoice.InvoiceId == Guid.Empty)
            throw new Exception("InvoiceId is required to create a journal entry");

        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = invoice.OrganizationId,
            OfficeIds = invoice.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Invoice,
            SourceId = invoice.InvoiceId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var journalEntry = await BuildJournalEntryFromInvoiceAsync(invoice, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    public async Task<JournalEntry> CreateJournalEntryFromPaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, Guid currentUser)
    {
        if (paymentLedgerLine.LedgerLineId == Guid.Empty)
            throw new Exception("LedgerLineId is required to create a payment journal entry");

        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = invoice.OrganizationId,
            OfficeIds = invoice.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.InvoicePayment,
            SourceId = paymentLedgerLine.LedgerLineId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        var existingEntry = existingEntries.FirstOrDefault(e => !e.IsVoided);
        if (existingEntry != null)
            return existingEntry;

        var journalEntry = await BuildJournalEntryFromPaymentAsync(invoice, paymentLedgerLine, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    public async Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentAsync(InvoicePayment invoicePayment, Guid currentUser)
    {
        var journalEntries = new List<JournalEntry>();

        foreach (var paymentApplication in invoicePayment.PaymentApplications)
        {
            var journalEntry = await CreateJournalEntryFromPaymentAsync(
                paymentApplication.Invoice,
                paymentApplication.PaymentLedgerLine,
                currentUser);
            journalEntries.Add(journalEntry);
        }

        return journalEntries;
    }
    #endregion

    #region Journal Entry
    async Task<JournalEntry> BuildJournalEntryFromInvoiceAsync(Invoice invoice, Guid currentUser)
    {
        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);
        var chartOfAccounts = await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var accountsReceivableAccountId = ResolveAccountsReceivableAccountId(chartOfAccounts, invoice.OfficeId);
        var defaultIncomeAccountId = ResolveDefaultIncomeAccountId(chartOfAccounts, invoice.OfficeId);
        var propertyId = await ResolveInvoicePropertyIdAsync(invoice);

        var chargeLines = invoice.LedgerLines
            .Where(l => l.Amount != 0)
            .Where(l =>
            {
                costCodeById.TryGetValue(l.CostCodeId, out var costCode);
                return !IsPaymentLedgerLine(costCode);
            })
            .OrderBy(l => l.LineNumber)
            .ToList();

        if (chargeLines.Count == 0)
            throw new Exception("Invoice has no charge ledger lines to create a journal entry");

        var totalAmount = chargeLines.Sum(l => l.Amount);
        if (totalAmount == 0)
            throw new Exception("Invoice charge total is zero");

        if (invoice.AccountingPeriod == default)
            throw new Exception("AccountingPeriod is required to create a journal entry for an invoice");

        var transactionDate = invoice.InvoiceDate != default ? invoice.InvoiceDate : invoice.AccountingPeriod;
        var postingDate = invoice.AccountingPeriod;
        var memo = string.IsNullOrWhiteSpace(invoice.Notes)
            ? $"Invoice {invoice.InvoiceCode}"
            : invoice.Notes.Trim();

        var journalEntryLines = new List<JournalEntryLine>();
        var accountsReceivableAmount = Math.Abs(totalAmount);
        journalEntryLines.Add(new JournalEntryLine
        {
            ChartOfAccountId = accountsReceivableAccountId,
            ReservationId = invoice.ReservationId,
            PropertyId = propertyId,
            ContactId = invoice.ContactId,
            Debit = totalAmount > 0 ? accountsReceivableAmount : 0,
            Credit = totalAmount < 0 ? accountsReceivableAmount : 0,
            Memo = $"Accounts Receivable - {invoice.InvoiceCode}",
            CreatedBy = currentUser
        });

        foreach (var line in chargeLines)
        {
            costCodeById.TryGetValue(line.CostCodeId, out var costCode);
            var incomeAccountId = ResolveChartOfAccountIdForCostCode(costCode, chartOfAccounts, invoice.OfficeId, defaultIncomeAccountId);
            var lineAmount = Math.Abs(line.Amount);

            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = incomeAccountId,
                CostCodeId = line.CostCodeId,
                ReservationId = line.ReservationId ?? invoice.ReservationId,
                PropertyId = propertyId,
                ContactId = invoice.ContactId,
                Debit = line.Amount < 0 ? lineAmount : 0,
                Credit = line.Amount > 0 ? lineAmount : 0,
                Memo = line.Description,
                CreatedBy = currentUser
            });
        }

        return new JournalEntry
        {
            OrganizationId = invoice.OrganizationId,
            OfficeId = invoice.OfficeId,
            TransactionDate = transactionDate,
            PostingDate = postingDate,
            SourceTypeId = (int)SourceType.Invoice,
            SourceId = invoice.InvoiceId,
            Memo = memo,
            JournalEntryLines = journalEntryLines,
            CreatedBy = currentUser
        };
    }

    async Task<JournalEntry> BuildJournalEntryFromPaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, Guid currentUser)
    {
        if (paymentLedgerLine.Amount == 0)
            throw new Exception("Payment amount cannot be zero");

        if (paymentLedgerLine.LedgerLineDate == default)
            throw new Exception("Payment date is required to create a payment journal entry");

        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);
        if (!costCodeById.TryGetValue(paymentLedgerLine.CostCodeId, out var paymentCostCode) || !IsPaymentLedgerLine(paymentCostCode))
            throw new Exception("Payment ledger line must use a payment cost code");

        var chartOfAccounts = await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var accountsReceivableAccountId = ResolveAccountsReceivableAccountId(chartOfAccounts, invoice.OfficeId);
        var undepositedFundsAccountId = ResolveUndepositedFundsAccountId(chartOfAccounts, invoice.OfficeId);
        var propertyId = await ResolveInvoicePropertyIdAsync(invoice);
        var reservationId = paymentLedgerLine.ReservationId ?? invoice.ReservationId;

        var amount = Math.Abs(paymentLedgerLine.Amount);
        var transactionDate = paymentLedgerLine.LedgerLineDate;
        var postingDate = paymentLedgerLine.LedgerLineDate;
        var memo = string.IsNullOrWhiteSpace(paymentLedgerLine.Description)
            ? $"Invoice Payment - {invoice.InvoiceCode}"
            : paymentLedgerLine.Description.Trim();

        JournalEntryLine undepositedFundsLine;
        JournalEntryLine accountsReceivableLine;

        if (paymentLedgerLine.Amount > 0)
        {
            undepositedFundsLine = new JournalEntryLine
            {
                ChartOfAccountId = undepositedFundsAccountId,
                CostCodeId = paymentLedgerLine.CostCodeId,
                ReservationId = reservationId,
                PropertyId = propertyId,
                ContactId = invoice.ContactId,
                Debit = amount,
                Credit = 0,
                Memo = memo,
                CreatedBy = currentUser
            };
            accountsReceivableLine = new JournalEntryLine
            {
                ChartOfAccountId = accountsReceivableAccountId,
                ReservationId = reservationId,
                PropertyId = propertyId,
                ContactId = invoice.ContactId,
                Debit = 0,
                Credit = amount,
                Memo = $"Accounts Receivable - {invoice.InvoiceCode}",
                CreatedBy = currentUser
            };
        }
        else
        {
            undepositedFundsLine = new JournalEntryLine
            {
                ChartOfAccountId = undepositedFundsAccountId,
                CostCodeId = paymentLedgerLine.CostCodeId,
                ReservationId = reservationId,
                PropertyId = propertyId,
                ContactId = invoice.ContactId,
                Debit = 0,
                Credit = amount,
                Memo = memo,
                CreatedBy = currentUser
            };
            accountsReceivableLine = new JournalEntryLine
            {
                ChartOfAccountId = accountsReceivableAccountId,
                ReservationId = reservationId,
                PropertyId = propertyId,
                ContactId = invoice.ContactId,
                Debit = amount,
                Credit = 0,
                Memo = $"Accounts Receivable - {invoice.InvoiceCode}",
                CreatedBy = currentUser
            };
        }

        return new JournalEntry
        {
            OrganizationId = invoice.OrganizationId,
            OfficeId = invoice.OfficeId,
            TransactionDate = transactionDate,
            PostingDate = postingDate,
            SourceTypeId = (int)SourceType.InvoicePayment,
            SourceId = paymentLedgerLine.LedgerLineId,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { undepositedFundsLine, accountsReceivableLine },
            CreatedBy = currentUser
        };
    }
    #endregion

    #region Static Helpers
    static bool IsPaymentLedgerLine(CostCode? costCode)
    {
        return costCode?.TransactionType == TransactionType.Payment;
    }

    static int ResolveAccountsReceivableAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.AccountsReceivable)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Accounts Receivable chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    static int ResolveDefaultIncomeAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Income)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Income chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    static int ResolveUndepositedFundsAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.OtherCurrentAsset)
            .Where(a =>
                a.Name.Contains("Undeposited", StringComparison.OrdinalIgnoreCase) ||
                a.AccountNo.Contains("Undeposited", StringComparison.OrdinalIgnoreCase))
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

    static int ResolveDefaultBankAccountId(List<ChartOfAccount> chartOfAccounts, int officeId)
    {
        var account = chartOfAccounts
            .Where(a => a.OfficeId == officeId && a.AccountType == AccountType.Bank)
            .OrderBy(a => a.AccountId)
            .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Bank chart of account is configured for office {officeId}");

        return account.AccountId;
    }

    static int ResolveChartOfAccountIdForCostCode(
        CostCode? costCode,
        List<ChartOfAccount> chartOfAccounts,
        int officeId,
        int defaultAccountId)
    {
        if (costCode == null || string.IsNullOrWhiteSpace(costCode.Code))
            return defaultAccountId;

        var accountCode = NormalizeAccountCode(costCode.Code);
        if (string.IsNullOrWhiteSpace(accountCode))
            return defaultAccountId;

        var account = chartOfAccounts.FirstOrDefault(a =>
            a.OfficeId == officeId &&
            NormalizeAccountCode(a.AccountNo).Equals(accountCode, StringComparison.OrdinalIgnoreCase));

        return account?.AccountId ?? defaultAccountId;
    }

    static string NormalizeAccountCode(string value)
    {
        return string.Join(' ',
            value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    async Task<Guid?> ResolveInvoicePropertyIdAsync(Invoice invoice)
    {
        if (invoice.PropertyId.HasValue && invoice.PropertyId != Guid.Empty)
            return invoice.PropertyId;

        if (!invoice.ReservationId.HasValue || invoice.ReservationId == Guid.Empty)
            return null;

        if (invoice.ReservationId == SystemOrganization)
            return null;

        var reservation = await _reservationRepository.GetReservationByIdAsync(invoice.ReservationId.Value, invoice.OrganizationId);
        if (reservation == null || reservation.PropertyId == Guid.Empty)
            return null;

        return reservation.PropertyId;
    }
    #endregion
}

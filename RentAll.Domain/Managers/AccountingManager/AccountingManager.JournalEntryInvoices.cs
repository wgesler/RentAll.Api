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

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var journalEntry = await BuildJournalEntryFromInvoiceAsync(invoice, chartOfAccounts, accountingOffice, currentUser);
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

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var journalEntry = await BuildJournalEntryFromPaymentAsync(invoice, paymentLedgerLine, chartOfAccounts, accountingOffice, currentUser);
        return await CreateJournalEntryAsync(journalEntry);
    }

    public async Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentAsync(InvoicePayment invoicePayment, Guid currentUser)
    {
        var journalEntries = new List<JournalEntry>();

        if (invoicePayment.PaymentApplications.Count == 0
            || !await IsAccountingFeatureEnabledAsync(invoicePayment.PaymentApplications[0].Invoice.OrganizationId))
            return journalEntries;

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
    async Task<JournalEntry> BuildJournalEntryFromInvoiceAsync(Invoice invoice, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser)
    {
        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);
        var accountsReceivableAccountId = GetAccountsReceivableAccountId(chartOfAccounts, invoice.OfficeId, accountingOffice);
        var defaultIncomeAccountId = GetTenantIncomeAccountId(chartOfAccounts, invoice.OfficeId, accountingOffice);
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
        var (accountsReceivableDebit, accountsReceivableCredit) = SignedAmountToDebitCredit(totalAmount, positiveIsDebit: true);
        journalEntryLines.Add(new JournalEntryLine
        {
            ChartOfAccountId = accountsReceivableAccountId,
            ReservationId = invoice.ReservationId,
            PropertyId = propertyId,
            ContactId = invoice.ContactId,
            Debit = accountsReceivableDebit,
            Credit = accountsReceivableCredit,
            Memo = $"Accounts Receivable - {invoice.InvoiceCode}",
            CreatedBy = currentUser
        });

        foreach (var line in chargeLines)
        {
            costCodeById.TryGetValue(line.CostCodeId, out var costCode);
            var incomeAccountId = GetChartOfAccountIdForCostCode(
                chartOfAccounts,
                invoice.OfficeId,
                costCode,
                defaultIncomeAccountId);
            var (incomeDebit, incomeCredit) = SignedAmountToDebitCredit(line.Amount, positiveIsDebit: false);

            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = incomeAccountId,
                CostCodeId = line.CostCodeId,
                ReservationId = line.ReservationId ?? invoice.ReservationId,
                PropertyId = propertyId,
                ContactId = invoice.ContactId,
                Debit = incomeDebit,
                Credit = incomeCredit,
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

    async Task<JournalEntry> BuildJournalEntryFromPaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser)
    {
        if (paymentLedgerLine.Amount == 0)
            throw new Exception("Payment amount cannot be zero");

        if (paymentLedgerLine.LedgerLineDate == default)
            throw new Exception("Payment date is required to create a payment journal entry");

        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);
        if (!costCodeById.TryGetValue(paymentLedgerLine.CostCodeId, out var paymentCostCode) || !IsPaymentLedgerLine(paymentCostCode))
            throw new Exception("Payment ledger line must use a payment cost code");

        var accountsReceivableAccountId = GetAccountsReceivableAccountId(chartOfAccounts, invoice.OfficeId, accountingOffice);
        var undepositedFundsAccountId = GetUndepositedFundsAccountId(chartOfAccounts, invoice.OfficeId, accountingOffice);
        var propertyId = await ResolveInvoicePropertyIdAsync(invoice);
        var reservationId = paymentLedgerLine.ReservationId ?? invoice.ReservationId;

        var amount = paymentLedgerLine.Amount;
        var transactionDate = paymentLedgerLine.LedgerLineDate;
        var postingDate = paymentLedgerLine.LedgerLineDate;
        var memo = string.IsNullOrWhiteSpace(paymentLedgerLine.Description)
            ? $"Invoice Payment - {invoice.InvoiceCode}"
            : paymentLedgerLine.Description.Trim();

        var (undepositedFundsDebit, undepositedFundsCredit) = SignedAmountToDebitCredit(amount, positiveIsDebit: true);
        var (accountsReceivableDebit, accountsReceivableCredit) = SignedAmountToDebitCredit(-amount, positiveIsDebit: true);

        var undepositedFundsLine = new JournalEntryLine
        {
            ChartOfAccountId = undepositedFundsAccountId,
            CostCodeId = paymentLedgerLine.CostCodeId,
            ReservationId = reservationId,
            PropertyId = propertyId,
            ContactId = invoice.ContactId,
            Debit = undepositedFundsDebit,
            Credit = undepositedFundsCredit,
            Memo = memo,
            CreatedBy = currentUser
        };
        var accountsReceivableLine = new JournalEntryLine
        {
            ChartOfAccountId = accountsReceivableAccountId,
            ReservationId = reservationId,
            PropertyId = propertyId,
            ContactId = invoice.ContactId,
            Debit = accountsReceivableDebit,
            Credit = accountsReceivableCredit,
            Memo = $"Accounts Receivable - {invoice.InvoiceCode}",
            CreatedBy = currentUser
        };

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

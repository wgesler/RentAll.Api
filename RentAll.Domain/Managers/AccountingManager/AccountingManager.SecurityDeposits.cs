using RentAll.Domain.Enums;
using RentAll.Domain.Helpers;
using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private class SecurityDepositContext
    {
        public Guid OrganizationId { get; init; }
        public int OfficeId { get; init; }
        public Guid ReservationId { get; init; }
        public string ReservationCode { get; init; } = string.Empty;
        public decimal CollectedAmount { get; init; }
        public decimal OutstandingAmount { get; init; }
        public decimal RefundableAmount { get; init; }
        public int ChartOfAccountId { get; init; }
        public PaymentType PaymentType { get; init; }
        public DateOnly PaymentDate { get; init; }
        public Guid CurrentUser { get; init; }
        public string Memo { get; init; } = string.Empty;
        public JournalEntryLineContext LineContext { get; init; }
    }

    #region Triggers

    public async Task<UnreturnedSecurityDepositsResult> GetUnreturnedSecurityDepositsAsync(Guid organizationId, string officeAccess, int? officeId = null)
    {
        var rows = (await _reservationRepository.GetUnreturnedSecurityDepositsAsync(organizationId, officeAccess)).ToList();
        if (officeId is > 0)
            rows = rows.Where(row => row.OfficeId == officeId.Value).ToList();

        await EnrichSecurityDepositJournalEntryDataAsync(organizationId, rows);

        // Drop reservations marked DepositType=SecurityDeposit that never got a deposit charge/collection.
        rows = rows
            .Where(row => RoundSecurityDepositAmount(row.Deposit) > 0m
                || RoundSecurityDepositAmount(row.CollectedAmount) > 0m)
            .ToList();

        var officeIds = ResolveSecurityDepositSummaryOfficeIds(officeAccess, officeId, rows);
        var totalDepositsOwed = RoundSecurityDepositAmount(rows.Sum(row => row.Deposit));

        decimal escrowBalance = 0m;
        var escrowAccountLabels = new List<string>();
        foreach (var summaryOfficeId in officeIds)
        {
            var (accountLabel, balance) = await LoadEscrowSecurityDepositAccountBalanceAsync(organizationId, summaryOfficeId);
            escrowBalance = RoundSecurityDepositAmount(escrowBalance + balance);
            if (!string.IsNullOrWhiteSpace(accountLabel))
                escrowAccountLabels.Add(accountLabel.Trim());
        }

        return new UnreturnedSecurityDepositsResult
        {
            Rows = rows,
            TotalDepositsOwed = totalDepositsOwed,
            EscrowBalance = escrowBalance,
            Discrepancy = RoundSecurityDepositAmount(escrowBalance - totalDepositsOwed),
            EscrowAccountLabel = ResolveEscrowAccountLabel(escrowAccountLabels, officeIds.Count)
        };
    }

    public async Task<SecurityDepositDetailResult> GetSecurityDepositDetailAsync(Guid reservationId, Guid organizationId, string officeAccess)
    {
        _ = officeAccess;
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

        if (reservation.DepositType != DepositType.Deposit)
            throw new Exception("Reservation does not have a security deposit");

        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, organizationId)
            ?? throw new Exception("Property not found");

        var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, organizationId);
        var departure = BuildSecurityDepositDeparture(reservation, property, office);
        var rows = new List<ReservationDeparture> { departure };
        await EnrichSecurityDepositJournalEntryDataAsync(organizationId, rows);
        departure = rows[0];

        var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, departure.OfficeId);
        var invoices = await GetInvoicesForReservationsAsync(organizationId, departure.OfficeId, [reservationId]);
        var securityDepositInvoices = FindSecurityDepositInvoicesForReservation(invoices, costCodeById);
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, departure.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, departure.OfficeId, accountingOffice);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, departure.OfficeId, accountingOffice);

        var securityDepositCharges = BuildSecurityDepositChargeLines(securityDepositInvoices, costCodeById);
        var applicationPaymentIds = (await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices))
            .Select(payment => payment.PaymentId)
            .ToHashSet();
        var outstandingCharges = BuildOutstandingChargeLines(invoices, costCodeById, applicationPaymentIds);

        var securityDepositPayments = new List<SecurityDepositDetailLine>();
        foreach (var securityDepositInvoice in securityDepositInvoices)
        {
            var journalEntriesBySourceId = await LoadJournalEntriesForInvoiceAsync(organizationId, departure.OfficeId, securityDepositInvoice);
            var paymentAmountsByLedgerLineId = CalculateSecurityDepositPaymentAmountsByLedgerLine(securityDepositInvoice, costCodeById);
            foreach (var paymentLine in securityDepositInvoice.LedgerLines ?? [])
            {
                if (!paymentAmountsByLedgerLineId.TryGetValue(paymentLine.LedgerLineId, out var appliedAmount) || appliedAmount <= 0m)
                    continue;

                var (journalEntryId, journalEntryCode) = ResolvePaymentJournalEntryForLedgerLine(
                    paymentLine.LedgerLineId,
                    journalEntriesBySourceId,
                    undepositedFundsAccountId);
                securityDepositPayments.Add(CreateSecurityDepositDetailLine(
                    securityDepositInvoice,
                    paymentLine,
                    appliedAmount,
                    journalEntryId,
                    journalEntryCode));
            }
        }

        var returnJournalEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = departure.OfficeId.ToString(CultureInfo.InvariantCulture),
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = reservationId,
            IncludeUnposted = true
        });

        var returnPayments = (returnJournalEntries ?? [])
            .Where(IsTenantSecurityDepositReturnJournalEntry)
            .Select(entry => new SecurityDepositDetailReturnLine
            {
                JournalEntryId = entry.JournalEntryId,
                JournalEntryCode = entry.JournalEntryCode?.Trim() ?? string.Empty,
                TransactionDate = entry.TransactionDate,
                Memo = entry.Memo?.Trim() ?? string.Empty,
                Amount = RoundSecurityDepositAmount(CalculateSecurityDepositAccountsReceivableDebit(entry, accountsReceivableAccountId))
            })
            .Where(line => line.Amount != 0m)
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode)
            .ToList();

        var balanceAmount = RoundSecurityDepositAmount(Math.Max(0m, departure.CollectedAmount - departure.OwedAmount));
        var remainingReturnAmount = RoundSecurityDepositAmount(Math.Max(0m, balanceAmount - departure.ReturnedAmount));

        return new SecurityDepositDetailResult
        {
            Reservation = departure,
            DepositAmount = RoundSecurityDepositAmount(departure.Deposit),
            CollectedAmount = RoundSecurityDepositAmount(departure.CollectedAmount),
            OwedAmount = RoundSecurityDepositAmount(departure.OwedAmount),
            BalanceAmount = balanceAmount,
            ReturnedAmount = RoundSecurityDepositAmount(departure.ReturnedAmount),
            RemainingReturnAmount = remainingReturnAmount,
            SecurityDepositCharges = securityDepositCharges,
            OutstandingCharges = outstandingCharges,
            SecurityDepositPayments = securityDepositPayments,
            ReturnPayments = returnPayments
        };
    }

    public async Task<Reservation> ApplySecurityDepositReturnAsync(Guid reservationId, Guid organizationId, string officeAccess, int chartOfAccountId, string description, decimal amount, DateOnly paymentDate, PaymentType paymentType, Guid currentUser)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

        ValidateSecurityDepositReturnReservation(reservationId, reservation, description);

        if (reservation.DepositReturned)
            throw new Exception("Security deposit has already been returned");

        if (reservation.DepositType != DepositType.Deposit)
            throw new Exception("Reservation does not have a security deposit");

        if (paymentDate == default)
            throw new Exception("Payment date is required for security deposit return");

        var collectedAmount = await GetSecurityDepositCollectedAmountAsync(organizationId, reservation.ReservationId, reservation.OfficeId);
        if (collectedAmount <= 0)
            throw new Exception("No security deposit has been collected for this reservation");

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
        {
            await _reservationRepository.MarkDepositReturnedAsync(reservation.ReservationId, organizationId, currentUser);
            reservation.DepositReturned = true;
            return reservation;
        }

        // Collect invoice amounts, deposits, and outstanding charges
        var (_, costCodeById) = await LoadOfficeCostCodeContextAsync(organizationId, reservation.OfficeId);
        var invoices = await GetInvoicesForReservationsAsync(organizationId, reservation.OfficeId, [reservation.ReservationId]);
        var outstandingAmount = CalculateReservationOutstandingOwedAmount(invoices, costCodeById);
        var hasOutstandingCharges = outstandingAmount > 0m;
        var refundToCreditCard = paymentType == PaymentType.CreditCardRefund;

        var contactId = reservation.ContactIds.FirstOrDefault(id => id != Guid.Empty) is { } id && id != Guid.Empty ? id : reservation.CompanyId ?? Guid.Empty;
        var depositContext = new SecurityDepositContext
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            ReservationId = reservation.ReservationId,
            ReservationCode = reservation.ReservationCode,
            CollectedAmount = collectedAmount,
            OutstandingAmount = outstandingAmount,
            RefundableAmount = RoundSecurityDepositAmount(collectedAmount - outstandingAmount),
            ChartOfAccountId = chartOfAccountId,
            PaymentType = paymentType,
            PaymentDate = paymentDate,
            CurrentUser = currentUser,
            Memo = $"{reservation.ReservationCode.Trim()}: Security Deposit",
            LineContext = new JournalEntryLineContext(reservation.PropertyId, reservation.PropertyCode, reservation.ReservationId, reservation.ReservationCode, contactId, reservation.ContactName)
        };

        Payment? invoicePayment = null;
        if (hasOutstandingCharges)
            invoicePayment = await CreateSecurityDepositInvoicePaymentAsync(depositContext, invoices, costCodeById, officeAccess);

        try
        {
            switch (hasOutstandingCharges, refundToCreditCard)
            {
                case (false, false):
                    await PostSecurityDepositRefundByBankAsync(depositContext);
                    break;
                case (false, true):
                    await PostSecurityDepositRefundByCreditCardAsync(depositContext);
                    break;
                case (true, false):
                    await PostSecurityDepositRefundWithChargesByBankAsync(depositContext);
                    break;
                case (true, true):
                    await PostSecurityDepositRefundWithChargesByCreditCardAsync(depositContext);
                    break;
            }
        }
        catch
        {
            if (invoicePayment != null)
                await DeletePaymentAsync(invoicePayment.PaymentId, organizationId, currentUser);
            throw;
        }

        await _reservationRepository.MarkDepositReturnedAsync(reservation.ReservationId, organizationId, currentUser);
        reservation.DepositReturned = true;

        return reservation;
    }

    public async Task<Reservation> UndoSecurityDepositAsync(Guid reservationId, Guid organizationId, string officeAccess, Guid currentUser)
    {
        _ = officeAccess;
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, organizationId)
            ?? throw new Exception("Property not found");

        var departure = BuildSecurityDepositDeparture(reservation, property);
        var rows = new List<ReservationDeparture> { departure };
        await EnrichSecurityDepositJournalEntryDataAsync(organizationId, rows);
        departure = rows[0];

        var depositReturned = reservation.DepositReturned;
        var depositComplete = IsSecurityDepositComplete(departure);
        if (!depositReturned && !depositComplete)
            throw new Exception("Nothing to undo for this security deposit");

        var entries = await GetSecurityDepositJournalEntriesForReservationAsync(organizationId, reservation);
        var didUndo = false;

        if (depositComplete)
        {
            foreach (var entry in entries.Where(IsSecurityDepositTransferJournalEntry))
            {
                await DeleteJournalEntryAsync(entry.JournalEntryId, organizationId);
                didUndo = true;
            }
        }

        if (depositReturned)
        {
            await DeleteSecurityDepositApplicationPaymentsAsync(organizationId, reservation, currentUser);
            foreach (var entry in entries.Where(entry => !IsSecurityDepositTransferJournalEntry(entry)))
            {
                await DeleteJournalEntryAsync(entry.JournalEntryId, organizationId);
                didUndo = true;
            }

            await _reservationRepository.ClearDepositReturnedAsync(reservation.ReservationId, organizationId, currentUser);
            reservation.DepositReturned = false;
            didUndo = true;
        }

        if (!didUndo)
            throw new Exception("Nothing to undo for this security deposit");

        return reservation;
    }

    public async Task SetSecurityDepositPaymentProcessedAsync(Guid reservationId, Guid organizationId, bool paymentProcessed, Guid currentUser)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

        if (paymentProcessed && !reservation.DepositReturned)
            throw new Exception("Return the security deposit before marking payment processed.");

        if (paymentProcessed)
            await _reservationRepository.MarkPaymentProcessedAsync(reservation.ReservationId, organizationId, currentUser);
        else
            await _reservationRepository.ClearPaymentProcessedAsync(reservation.ReservationId, organizationId, currentUser);
    }

    #endregion

    #region Journal Entry

    private async Task PostSecurityDepositRefundByBankAsync(SecurityDepositContext context)
    {
        var journalEntry = await BuildJournalEntryForSecurityDepositToAccountsReceivable(context);

        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-AR-TO-ESCROW-JE-ACCOUNTS
        // Line 1 — Selected bank (chartOfAccountId): Credit returned amount / Debit 0 (positive return) or reverse for negative.
        // Line 2 — Accounts Receivable (GetDefaultAccountsReceivable): Debit returned amount / Credit 0 (positive return) or reverse for negative.
        // END SECURITY-DEPOSIT-AR-TO-ESCROW-JE-ACCOUNTS

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, context.OfficeId, accountingOffice);
        AppendSecurityDepositReturnedAmountLines(journalEntry, context, accountsReceivableAccountId);
        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
    }

    private async Task PostSecurityDepositRefundByCreditCardAsync(SecurityDepositContext context)
    {
        var journalEntry = await BuildJournalEntryForSecurityDepositToAccountsReceivable(context);

        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-REFUND-BY-CREDIT-CARD-JE-ACCOUNTS
        // Line 1 — Escrow Security Deposit (GetDefaultEscrowSecurityDepositAccount): Credit collected amount / Debit 0.
        // Line 2 — Undeposited Funds (GetDefaultUndepositedFunds): Debit collected amount / Credit 0.
        // Line 3 — Selected bank (chartOfAccountId): Credit returned amount / Debit 0 (positive return) or reverse for negative.
        // Line 4 — Accounts Receivable (GetDefaultAccountsReceivable): Debit returned amount / Credit 0 (positive return) or reverse for negative.
        // END SECURITY-DEPOSIT-REFUND-BY-CREDIT-CARD-JE-ACCOUNTS

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, context.OfficeId, accountingOffice);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, context.OfficeId, accountingOffice);

        var escrowSecurityDepositLine = new JournalEntryLine
        {
            ChartOfAccountId = escrowSecurityDepositAccountId,
            Debit = 0,
            Credit = context.CollectedAmount
        };
        ApplySecurityDepositLine(escrowSecurityDepositLine, context);

        var undepositedFundsDebitLine = new JournalEntryLine
        {
            ChartOfAccountId = undepositedFundsAccountId,
            Debit = context.CollectedAmount,
            Credit = 0
        };
        ApplySecurityDepositLine(undepositedFundsDebitLine, context);

        journalEntry.JournalEntryLines.AddRange([escrowSecurityDepositLine, undepositedFundsDebitLine]);
        AppendSecurityDepositReturnedAmountLines(journalEntry, context, accountsReceivableAccountId);
        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
    }

    private async Task PostSecurityDepositRefundWithChargesByBankAsync(SecurityDepositContext context)
    {
        var journalEntry = await BuildJournalEntryForSecurityDepositToAccountsReceivable(context);

        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-REFUND-WITH-CHARGES-BY-BANK-JE-ACCOUNTS
        // Line 1 — Escrow Security Deposit (GetDefaultEscrowSecurityDepositAccount): Credit outstanding charges / Debit 0.
        // Line 2 — Business bank (GetDefaultBankAccount): Debit outstanding charges / Credit 0.
        // Line 3 — Selected bank (chartOfAccountId): Credit returned amount / Debit 0 (positive return) or reverse for negative.
        // Line 4 — Accounts Receivable (GetDefaultAccountsReceivable): Debit returned amount / Credit 0 (positive return) or reverse for negative.
        // END SECURITY-DEPOSIT-REFUND-WITH-CHARGES-BY-BANK-JE-ACCOUNTS

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var businessAccountId = GetDefaultBankAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, context.OfficeId, accountingOffice);
        var chargeAmount = RoundSecurityDepositAmount(context.OutstandingAmount);

        var chargeEscrowLine = new JournalEntryLine
        {
            ChartOfAccountId = escrowSecurityDepositAccountId,
            Debit = 0,
            Credit = chargeAmount
        };
        ApplySecurityDepositLine(chargeEscrowLine, context);

        var businessBankLine = new JournalEntryLine
        {
            ChartOfAccountId = businessAccountId,
            Debit = chargeAmount,
            Credit = 0
        };
        ApplySecurityDepositLine(businessBankLine, context);

        journalEntry.JournalEntryLines.AddRange([chargeEscrowLine, businessBankLine]);
        AppendSecurityDepositReturnedAmountLines(journalEntry, context, accountsReceivableAccountId);
        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
    }

    private async Task PostSecurityDepositRefundWithChargesByCreditCardAsync(SecurityDepositContext context)
    {
        var journalEntry = await BuildJournalEntryForSecurityDepositToAccountsReceivable(context);

        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-REFUND-WITH-CHARGES-BY-CREDIT-CARD-JE-ACCOUNTS
        // Line 1 — Escrow Security Deposit (GetDefaultEscrowSecurityDepositAccount): Credit collected amount / Debit 0.
        // Line 2 — Undeposited Funds (GetDefaultUndepositedFunds): Debit refundable amount / Credit 0.
        // Line 3 — Business bank (GetDefaultBankAccount): Debit outstanding charges / Credit 0.
        // Line 4 — Selected bank (chartOfAccountId): Credit returned amount / Debit 0 (positive return) or reverse for negative.
        // Line 5 — Accounts Receivable (GetDefaultAccountsReceivable): Debit returned amount / Credit 0 (positive return) or reverse for negative.
        // END SECURITY-DEPOSIT-REFUND-WITH-CHARGES-BY-CREDIT-CARD-JE-ACCOUNTS

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var businessAccountId = GetDefaultBankAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, context.OfficeId, accountingOffice);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, context.OfficeId, accountingOffice);
        var chargeAmount = RoundSecurityDepositAmount(context.OutstandingAmount);

        var escrowSecurityDepositLine = new JournalEntryLine
        {
            ChartOfAccountId = escrowSecurityDepositAccountId,
            Debit = 0,
            Credit = context.CollectedAmount
        };
        ApplySecurityDepositLine(escrowSecurityDepositLine, context);

        var undepositedFundsDebitLine = new JournalEntryLine
        {
            ChartOfAccountId = undepositedFundsAccountId,
            Debit = context.RefundableAmount,
            Credit = 0
        };
        ApplySecurityDepositLine(undepositedFundsDebitLine, context);

        var businessBankLine = new JournalEntryLine
        {
            ChartOfAccountId = businessAccountId,
            Debit = chargeAmount,
            Credit = 0
        };
        ApplySecurityDepositLine(businessBankLine, context);

        journalEntry.JournalEntryLines.AddRange([escrowSecurityDepositLine, undepositedFundsDebitLine, businessBankLine]);
        AppendSecurityDepositReturnedAmountLines(journalEntry, context, accountsReceivableAccountId);
        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
    }

    private void AppendSecurityDepositReturnedAmountLines(JournalEntry journalEntry, SecurityDepositContext context, int accountsReceivableAccountId)
    {
        var amount = context.RefundableAmount;
        if (amount == 0m)
            return;

        var bankLine = new JournalEntryLine
        {
            ChartOfAccountId = context.ChartOfAccountId,
            Debit = amount < 0 ? Math.Abs(amount) : 0,
            Credit = amount > 0 ? amount : 0
        };
        ApplySecurityDepositLine(bankLine, context);

        var accountsReceivableLine = new JournalEntryLine
        {
            ChartOfAccountId = accountsReceivableAccountId,
            Debit = amount > 0 ? amount : 0,
            Credit = amount < 0 ? Math.Abs(amount) : 0
        };
        ApplySecurityDepositLine(accountsReceivableLine, context);

        journalEntry.JournalEntryLines.AddRange([bankLine, accountsReceivableLine]);
    }

    private void ApplySecurityDepositLine(JournalEntryLine line, SecurityDepositContext context)
    {
        line.Memo = context.Memo;
        line.CreatedBy = context.CurrentUser;
        ApplyJournalEntryLineContext(line, context.LineContext);
    }

    private async Task<JournalEntry> BuildJournalEntryForSecurityDepositToAccountsReceivable(SecurityDepositContext context)
    {
        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-TO-AR-JE-ACCOUNTS
        // Line 1 — Security Deposit cost code account (GetDefaultSecurityDepositAccount): Debit / Credit 0 (positive return) or reverse for negative.
        // Line 2 — Accounts Receivable (GetDefaultAccountsReceivable): Credit / Debit 0 (positive return) or reverse for negative.
        // END SECURITY-DEPOSIT-TO-AR-JE-ACCOUNTS

        var amount = context.CollectedAmount;
        var (office, costCodeById) = await LoadOfficeCostCodeContextAsync(context.OrganizationId, context.OfficeId);
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var securityDepositAccountId = GetDefaultSecurityDepositAccount(chartOfAccounts, context.OfficeId, office, costCodeById, accountingOffice);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, context.OfficeId, accountingOffice);

        var securityDepositLine = new JournalEntryLine
        {
            ChartOfAccountId = securityDepositAccountId,
            CostCodeId = office?.SecurityDepositCcId,
            Debit = amount > 0 ? amount : 0,
            Credit = amount < 0 ? Math.Abs(amount) : 0
        };
        ApplySecurityDepositLine(securityDepositLine, context);

        var accountsReceivableLine = new JournalEntryLine
        {
            ChartOfAccountId = accountsReceivableAccountId,
            Debit = amount < 0 ? Math.Abs(amount) : 0,
            Credit = amount > 0 ? amount : 0
        };
        ApplySecurityDepositLine(accountsReceivableLine, context);

        return ClassifyJournalEntry(new JournalEntry
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            TransactionDate = context.PaymentDate,
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = context.ReservationId,
            SourceCode = context.ReservationCode,
            Memo = context.Memo,
            PaymentTypeId = (int)context.PaymentType,
            JournalEntryLines = new List<JournalEntryLine> { securityDepositLine, accountsReceivableLine },
            CreatedBy = context.CurrentUser
        }, JournalEntryKind.SecurityDepositReturn, Perspective.Tenant);
    }

    #endregion

    #region Deposit Application

    private async Task<Payment> CreateSecurityDepositInvoicePaymentAsync(SecurityDepositContext context, IReadOnlyList<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById, string officeAccess)
    {
        var orderedInvoices = invoices
            .Where(invoice => invoice.IsActive)
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceCode)
            .ToList();
        var firstChargeInvoice = orderedInvoices
            .FirstOrDefault(invoice => CalculateInvoiceNonSecurityDepositNetDue(invoice, costCodeById) > 0m);
        if (firstChargeInvoice == null)
            throw new Exception("No outstanding invoice charges were found for the security deposit payment");

        var invoiceIds = orderedInvoices
            .SkipWhile(invoice => invoice.InvoiceId != firstChargeInvoice.InvoiceId)
            .Select(invoice => invoice.InvoiceId)
            .ToList();

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var (office, _) = await LoadOfficeCostCodeContextAsync(context.OrganizationId, context.OfficeId);
        var payment = new Payment
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            PaymentDate = context.PaymentDate,
            Amount = context.OutstandingAmount,
            CostCodeId = ResolveSecurityDepositPaymentCostCodeId(costCodeById),
            Description = context.Memo,
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)PaymentType.SecurityDeposit,
            ChartOfAccountId = GetDefaultSecurityDepositAccount(chartOfAccounts, context.OfficeId, office, costCodeById, accountingOffice),
            IsActive = true,
            CreatedBy = context.CurrentUser
        };

        return await ApplyInvoicePaymentAsync(payment, invoiceIds, null, officeAccess, context.CurrentUser);
    }

    private static int ResolveSecurityDepositPaymentCostCodeId(IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var costCode = costCodeById.Values
            .Where(code => code.IsActive && code.TransactionType == TransactionType.Payment)
            .OrderBy(code => code.CostCodeId)
            .FirstOrDefault();
        if (costCode == null)
            throw new Exception("No payment cost code is configured for this office");

        return costCode.CostCodeId;
    }

    private async Task DeleteSecurityDepositApplicationPaymentsAsync(Guid organizationId, Reservation reservation, Guid currentUser)
    {
        var invoices = await GetInvoicesForReservationsAsync(organizationId, reservation.OfficeId, [reservation.ReservationId]);
        var payments = await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices);
        foreach (var payment in payments)
            await DeletePaymentAsync(payment.PaymentId, organizationId, currentUser);
    }

    private async Task<List<Payment>> GetSecurityDepositApplicationPaymentsAsync(Guid organizationId, IReadOnlyList<Invoice> invoices)
    {
        var paymentIds = (invoices ?? [])
            .SelectMany(invoice => invoice.LedgerLines ?? [])
            .Select(line => line.PaymentId)
            .Where(paymentId => paymentId is { } id && id != Guid.Empty)
            .Select(paymentId => paymentId!.Value)
            .Distinct()
            .ToList();

        var payments = new List<Payment>();
        foreach (var paymentId in paymentIds)
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment?.PaymentTypeId == (int)PaymentType.SecurityDeposit)
                payments.Add(payment);
        }

        return payments;
    }

    #endregion

    #region Report Enrichment

    private async Task EnrichSecurityDepositJournalEntryDataAsync(Guid organizationId, IList<ReservationDeparture> rows)
    {
        if (rows.Count == 0)
            return;

        var reservationIds = rows.Select(row => row.ReservationId).Distinct().ToList();
        var officeIds = rows.Select(row => row.OfficeId).Where(id => id > 0).Distinct().ToList();

        var invoiceEnrichment = await GetReservationInvoiceEnrichmentAsync(organizationId, rows);
        var returnedByReservation = await GetSecurityDepositReturnPaidAmountsByReservationAsync(organizationId, reservationIds, officeIds);
        var transferredByReservation = await GetSecurityDepositTransferAmountsByReservationAsync(organizationId, reservationIds, officeIds);

        foreach (var row in rows)
        {
            row.OwedAmount = invoiceEnrichment.OwedByReservation.TryGetValue(row.ReservationId, out var owedAmount)
                ? owedAmount
                : 0m;
            row.BalanceAmount = invoiceEnrichment.BalanceByReservation.TryGetValue(row.ReservationId, out var balanceAmount)
                ? balanceAmount
                : 0m;
            if (returnedByReservation.TryGetValue(row.ReservationId, out var returnedActivity))
            {
                row.ReturnedAmount = returnedActivity.Amount;
                row.ReturnJournalEntryId = returnedActivity.JournalEntryId;
                row.ReturnJournalEntryCode = returnedActivity.JournalEntryCode;
            }
            else
            {
                row.ReturnedAmount = 0m;
                row.ReturnJournalEntryId = null;
                row.ReturnJournalEntryCode = string.Empty;
            }
            row.TransferredAmount = transferredByReservation.TryGetValue(row.ReservationId, out var transferredAmount)
                ? transferredAmount
                : 0m;

            if (invoiceEnrichment.SecurityDepositInfoByReservation.TryGetValue(row.ReservationId, out var securityDepositInfo))
            {
                row.Deposit = securityDepositInfo.DepositAmount;
                row.CollectedAmount = securityDepositInfo.CollectedAmount;
                row.PaidJournalEntryId = securityDepositInfo.PaidJournalEntryId;
                row.PaidJournalEntryCode = securityDepositInfo.PaidJournalEntryCode;
                row.InvoiceId = securityDepositInfo.DepositInvoiceId;
                row.InvoiceCode = securityDepositInfo.DepositInvoiceCode;
                continue;
            }

            row.Deposit = 0m;
            row.CollectedAmount = 0m;
            row.PaidJournalEntryId = null;
            row.PaidJournalEntryCode = string.Empty;
            row.InvoiceId = null;
            row.InvoiceCode = string.Empty;
        }
    }

    private async Task<ReservationInvoiceEnrichment> GetReservationInvoiceEnrichmentAsync(Guid organizationId, IList<ReservationDeparture> rows)
    {
        var enrichment = new ReservationInvoiceEnrichment
        {
            OwedByReservation = rows.Select(row => row.ReservationId).Distinct().ToDictionary(id => id, _ => 0m),
            BalanceByReservation = rows.Select(row => row.ReservationId).Distinct().ToDictionary(id => id, _ => 0m),
            SecurityDepositInfoByReservation = new Dictionary<Guid, SecurityDepositInvoiceInfo>()
        };

        if (rows.Count == 0)
            return enrichment;

        foreach (var row in rows)
        {
            var (owedAmount, balanceAmount, securityDepositInfo) = await LoadSecurityDepositInvoiceAmountsAsync(organizationId, row.ReservationId, row.OfficeId);
            enrichment.OwedByReservation[row.ReservationId] = owedAmount;
            enrichment.BalanceByReservation[row.ReservationId] = balanceAmount;
            if (securityDepositInfo != null)
                enrichment.SecurityDepositInfoByReservation[row.ReservationId] = securityDepositInfo;
        }

        return enrichment;
    }

    private class ReservationInvoiceEnrichment
    {
        public Dictionary<Guid, decimal> OwedByReservation { get; init; } = new();
        public Dictionary<Guid, decimal> BalanceByReservation { get; init; } = new();
        public Dictionary<Guid, SecurityDepositInvoiceInfo> SecurityDepositInfoByReservation { get; init; } = new();
    }

    private class SecurityDepositInvoiceInfo
    {
        public decimal DepositAmount { get; init; }
        public decimal CollectedAmount { get; init; }
        public Guid? PaidJournalEntryId { get; init; }
        public string PaidJournalEntryCode { get; init; } = string.Empty;
        public Guid? DepositInvoiceId { get; init; }
        public string DepositInvoiceCode { get; init; } = string.Empty;
    }

    #endregion

    #region Invoice Payment Allocation

    private class SecurityDepositPaymentAllocation
    {
        public decimal PaidAmount { get; init; }
    }

    private static SecurityDepositPaymentAllocation CalculateSecurityDepositPaymentAllocation(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var chargeLines = invoice.LedgerLines
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .OrderBy(line => line.LineNumber)
            .ToList();

        var paymentLines = invoice.LedgerLines
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById))
            .OrderBy(line => line.LedgerLineDate)
            .ThenBy(line => line.LineNumber)
            .ToList();

        if (chargeLines.Count == 0 || paymentLines.Count == 0)
            return new SecurityDepositPaymentAllocation();

        var appliedToCharges = chargeLines.Select(_ => 0m).ToArray();
        var securityDepositPaid = 0m;

        foreach (var payment in paymentLines)
        {
            var paymentRemaining = payment.Amount;
            if (paymentRemaining > 0m)
            {
                for (var i = 0; i < chargeLines.Count && paymentRemaining != 0m; i++)
                {
                    var chargeRemaining = chargeLines[i].Amount - appliedToCharges[i];
                    if (chargeRemaining <= 0m)
                        continue;

                    var apply = Math.Min(paymentRemaining, chargeRemaining);
                    appliedToCharges[i] += apply;
                    paymentRemaining -= apply;

                    if (IsRefundableSecurityDepositCharge(chargeLines[i], costCodeById) && apply > 0m)
                        securityDepositPaid += apply;
                }
            }
            else if (paymentRemaining < 0m)
            {
                var toReverse = Math.Abs(paymentRemaining);
                for (var i = chargeLines.Count - 1; i >= 0 && toReverse != 0m; i--)
                {
                    if (appliedToCharges[i] <= 0m)
                        continue;

                    var reverse = Math.Min(toReverse, appliedToCharges[i]);
                    appliedToCharges[i] -= reverse;
                    toReverse -= reverse;

                    if (IsRefundableSecurityDepositCharge(chargeLines[i], costCodeById) && reverse > 0m)
                        securityDepositPaid -= reverse;
                }
            }
        }

        return new SecurityDepositPaymentAllocation
        {
            PaidAmount = Math.Max(0m, securityDepositPaid)
        };
    }

    private static bool IsInvoiceChargeLedgerLine(LedgerLine line, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (!costCodeById.TryGetValue(line.CostCodeId, out var costCode))
            return false;

        return !IsPaymentLedgerLine(costCode);
    }

    private static bool IsInvoicePaymentLedgerLine(LedgerLine line, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (!costCodeById.TryGetValue(line.CostCodeId, out var costCode))
            return false;

        return IsPaymentLedgerLine(costCode);
    }

    private async Task<List<Invoice>> GetInvoicesForReservationsAsync(Guid organizationId, int officeId, IReadOnlyCollection<Guid> reservationIds)
    {
        if (reservationIds.Count == 0)
            return [];

        var invoicesById = new Dictionary<Guid, Invoice>();
        foreach (var reservationId in reservationIds.Distinct())
        {
            var reservationInvoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                ReservationId = reservationId,
                IncludeInactive = false,
                IncludePaid = true
            });

            foreach (var invoice in reservationInvoices)
                invoicesById[invoice.InvoiceId] = invoice;
        }

        return invoicesById.Values.ToList();
    }

    /// <summary>
    /// Matches invoice editor total due for rent/fees on a single invoice (charges minus payments, excluding SD charges).
    /// </summary>
    private static decimal CalculateInvoiceNonSecurityDepositNetDue(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlySet<Guid>? ignoredPaymentIds = null)
    {
        var chargeTotal = RoundSecurityDepositAmount((invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Where(line => !IsRefundableSecurityDepositCharge(line, costCodeById))
            .Sum(line => line.Amount));

        var paymentTotal = RoundSecurityDepositAmount((invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById))
            .Where(line => !IsIgnoredSecurityDepositPayment(line, ignoredPaymentIds))
            .Sum(line => line.Amount));

        return Math.Max(0m, RoundSecurityDepositAmount(chargeTotal - paymentTotal));
    }

    // Normal operation expects exactly one security-deposit invoice per reservation.
    // When more than one exists, callers sum deposit/paid/owed and keep the first invoice for JE display links.
    private static List<Invoice> FindSecurityDepositInvoicesForReservation(IReadOnlyList<Invoice> reservationInvoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return reservationInvoices
            .Where(invoice => InvoiceHasSecurityDepositChargeLine(invoice, costCodeById))
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceCode)
            .ToList();
    }

    private static bool InvoiceHasSecurityDepositChargeLine(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return (invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Any(line => IsRefundableSecurityDepositCharge(line, costCodeById));
    }

    private static decimal CalculateSecurityDepositChargeAmount(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return RoundSecurityDepositAmount((invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Where(line => IsRefundableSecurityDepositCharge(line, costCodeById))
            .Sum(line => line.Amount));
    }

    private static bool IsRefundableSecurityDepositCharge(LedgerLine line, IReadOnlyDictionary<int, CostCode> costCodeById)
        => costCodeById.TryGetValue(line.CostCodeId, out var costCode)
            && costCode.TransactionType == TransactionType.SecurityDeposit;

    private static bool IsIgnoredSecurityDepositPayment(LedgerLine line, IReadOnlySet<Guid>? ignoredPaymentIds)
        => ignoredPaymentIds != null
            && line.PaymentId is { } paymentId
            && paymentId != Guid.Empty
            && ignoredPaymentIds.Contains(paymentId);

    #endregion

    #region Escrow Summary

    private static List<int> ResolveSecurityDepositSummaryOfficeIds(string officeAccess, int? officeId, IReadOnlyList<ReservationDeparture> rows)
    {
        if (officeId is > 0)
            return [officeId.Value];

        var accessOfficeIds = ParseOfficeIdsFromAccess(officeAccess);
        if (accessOfficeIds.Count > 0)
            return accessOfficeIds;

        return rows
            .Select(row => row.OfficeId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static string ResolveEscrowAccountLabel(IReadOnlyList<string> accountLabels, int officeCount)
    {
        if (accountLabels.Count == 1)
            return accountLabels[0];

        if (accountLabels.Count > 1)
            return "Escrow Security Deposit";

        return officeCount == 1 ? "Escrow Security Deposit" : "Escrow Security Deposit";
    }

    private async Task<(string AccountLabel, decimal Balance)> LoadEscrowSecurityDepositAccountBalanceAsync(Guid organizationId, int officeId)
    {
        try
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            var accountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, officeId, accountingOffice);
            var account = chartOfAccounts.First(item => item.AccountId == accountId);
            var accountLabel = $"{account.AccountNo} {account.Name}".Trim();

            var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                ChartOfAccountId = accountId,
                IncludeUnposted = true,
                StartDate = null,
                EndDate = null
            });

            var balance = RoundSecurityDepositAmount(SumEscrowLiabilityAccountBalance(lines));
            return (accountLabel, balance);
        }
        catch
        {
            // Report header only — skip offices without escrow security-deposit account setup.
            return (string.Empty, 0m);
        }
    }

    private static decimal SumEscrowLiabilityAccountBalance(IEnumerable<JournalEntryLineSearchResult> lines)
        => (lines ?? []).Sum(line => line.Credit - line.Debit);

    #endregion

    #region Return Amounts

    private async Task<Dictionary<Guid, SecurityDepositReturnActivity>> GetSecurityDepositReturnPaidAmountsByReservationAsync(Guid organizationId, IReadOnlyCollection<Guid> reservationIds, IReadOnlyCollection<int> officeIds)
    {
        if (reservationIds.Count == 0 || officeIds.Count == 0)
            return new Dictionary<Guid, SecurityDepositReturnActivity>();

        var reservationIdSet = reservationIds.ToHashSet();
        var accountsReceivableAccountIdByOffice = await LoadAccountsReceivableAccountIdsByOfficeAsync(organizationId, officeIds);
        var entries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = string.Join(',', officeIds),
            SourceTypeId = (int)SourceType.SecurityDeposit,
            IncludeUnposted = true
        });

        return (entries ?? [])
            .Where(entry => entry.SourceId.HasValue && reservationIdSet.Contains(entry.SourceId.Value))
            .Where(IsTenantSecurityDepositReturnJournalEntry)
            .GroupBy(entry => entry.SourceId!.Value)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var returnEntry = group
                        .Where(IsSecurityDepositReturnJournalEntry)
                        .OrderByDescending(entry => entry.TransactionDate)
                        .ThenByDescending(entry => entry.JournalEntryCode)
                        .FirstOrDefault()
                        ?? group
                            .OrderByDescending(entry => entry.TransactionDate)
                            .ThenByDescending(entry => entry.JournalEntryCode)
                            .First();
                    return new SecurityDepositReturnActivity
                    {
                        Amount = RoundSecurityDepositAmount(group.Sum(entry => CalculateSecurityDepositAccountsReceivableDebit(entry, accountsReceivableAccountIdByOffice))),
                        JournalEntryId = returnEntry.JournalEntryId,
                        JournalEntryCode = returnEntry.JournalEntryCode?.Trim() ?? string.Empty
                    };
                });
    }

    private async Task<Dictionary<int, int>> LoadAccountsReceivableAccountIdsByOfficeAsync(Guid organizationId, IReadOnlyCollection<int> officeIds)
    {
        var accountIds = new Dictionary<int, int>();
        foreach (var officeId in officeIds.Where(id => id > 0).Distinct())
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            accountIds[officeId] = GetDefaultAccountsReceivable(chartOfAccounts, officeId, accountingOffice);
        }

        return accountIds;
    }

    private static decimal CalculateSecurityDepositAccountsReceivableDebit(JournalEntry journalEntry, IReadOnlyDictionary<int, int> accountsReceivableAccountIdByOffice)
    {
        if (!accountsReceivableAccountIdByOffice.TryGetValue(journalEntry.OfficeId, out var accountsReceivableAccountId))
            return 0m;

        return CalculateSecurityDepositAccountsReceivableDebit(journalEntry, accountsReceivableAccountId);
    }

    private static decimal CalculateSecurityDepositAccountsReceivableDebit(JournalEntry journalEntry, int accountsReceivableAccountId)
    {
        if (accountsReceivableAccountId <= 0)
            return 0m;

        return (journalEntry.JournalEntryLines ?? [])
            .Where(line => line.ChartOfAccountId == accountsReceivableAccountId)
            .Sum(line => line.Debit);
    }

    private sealed class SecurityDepositReturnActivity
    {
        public decimal Amount { get; init; }
        public Guid? JournalEntryId { get; init; }
        public string JournalEntryCode { get; init; } = string.Empty;
    }

    private async Task<Dictionary<Guid, decimal>> GetSecurityDepositTransferAmountsByReservationAsync(Guid organizationId, IReadOnlyCollection<Guid> reservationIds, IReadOnlyCollection<int> officeIds)
    {
        if (reservationIds.Count == 0 || officeIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        var reservationIdSet = reservationIds.ToHashSet();
        var entries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = string.Join(',', officeIds),
            SourceTypeId = (int)SourceType.SecurityDeposit,
            IncludeUnposted = true
        });

        return (entries ?? [])
            .Where(entry => entry.SourceId.HasValue && reservationIdSet.Contains(entry.SourceId.Value))
            .Where(IsSecurityDepositTransferJournalEntry)
            .GroupBy(entry => entry.SourceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => RoundSecurityDepositAmount(group.Sum(CalculateSecurityDepositReturnJournalEntryAmount)));
    }

    private static bool IsSecurityDepositComplete(ReservationDeparture departure)
    {
        var deposit = RoundSecurityDepositAmount(departure.Deposit);
        var collected = RoundSecurityDepositAmount(departure.CollectedAmount);
        var settled = RoundSecurityDepositAmount(departure.ReturnedAmount + departure.TransferredAmount);
        return deposit > 0m && collected > 0m && collected == settled;
    }

    private async Task<List<JournalEntry>> GetSecurityDepositJournalEntriesForReservationAsync(Guid organizationId, Reservation reservation)
    {
        var entries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(CultureInfo.InvariantCulture),
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = reservation.ReservationId,
            IncludeUnposted = true
        });

        return (entries ?? []).ToList();
    }

    private static bool IsSecurityDepositReturnJournalEntry(JournalEntry journalEntry)
        => journalEntry.SourceTypeId == (int)SourceType.SecurityDeposit
            && journalEntry.JournalEntryKindId == JournalEntryKind.SecurityDepositReturn;

    private static bool IsTenantSecurityDepositReturnJournalEntry(JournalEntry journalEntry)
        => !IsSecurityDepositTransferJournalEntry(journalEntry)
            && journalEntry.JournalEntryKindId != JournalEntryKind.SecurityDepositActual;

    private static bool IsSecurityDepositTransferJournalEntry(JournalEntry journalEntry)
    {
        if (journalEntry.JournalEntryKindId == JournalEntryKind.SecurityDepositTransfer)
            return true;

        var memo = journalEntry.Memo?.Trim() ?? string.Empty;
        return memo.Contains("Security Deposit Transfer", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal CalculateSecurityDepositReturnJournalEntryAmount(JournalEntry journalEntry)
    {
        var lines = journalEntry.JournalEntryLines ?? [];
        if (lines.Count == 0)
            return 0m;

        var totalDebit = lines.Sum(line => line.Debit);
        var totalCredit = lines.Sum(line => line.Credit);
        return Math.Max(totalDebit, totalCredit);
    }

    #endregion

    #region Helpers

    private static void ValidateSecurityDepositReturnReservation(Guid reservationId, Reservation reservation, string description)
    {
        if (reservation.ReservationId != reservationId)
            throw new Exception("Reservation mismatch for security deposit return");

        if (string.IsNullOrWhiteSpace(description))
            return;

        var descriptionReservationCode = ExtractReservationCodeFromReturnDescription(description);
        if (descriptionReservationCode == null)
            return;

        if (!EntityCodeFormatting.CodesMatch(descriptionReservationCode, reservation.ReservationCode))
            throw new Exception($"Security deposit return description reservation {descriptionReservationCode} does not match selected reservation {reservation.ReservationCode}.");
    }

    private static string? ExtractReservationCodeFromReturnDescription(string description)
    {
        var trimmed = description.Trim();
        if (trimmed.Length == 0)
            return null;

        var separatorIndex = trimmed.IndexOf(':');
        if (separatorIndex > 0)
            return trimmed[..separatorIndex].Trim();

        const string suffix = " Security Deposit Return";
        if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return trimmed[..^suffix.Length].Trim();

        return null;
    }

    private async Task<decimal> GetSecurityDepositCollectedAmountAsync(Guid organizationId, Guid reservationId, int officeId)
    {
        var (_, _, securityDepositInfo) = await LoadSecurityDepositInvoiceAmountsAsync(organizationId, reservationId, officeId);
        return securityDepositInfo?.CollectedAmount ?? 0m;
    }

    private async Task<(decimal OwedAmount, decimal BalanceAmount, SecurityDepositInvoiceInfo? SecurityDepositInfo)> LoadSecurityDepositInvoiceAmountsAsync(Guid organizationId, Guid reservationId, int officeId)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, officeId);
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice);
        var invoices = await GetInvoicesForReservationsAsync(organizationId, officeId, [reservationId]);
        var securityDepositInvoices = FindSecurityDepositInvoicesForReservation(invoices, costCodeById);
        if (securityDepositInvoices.Count == 0)
            return (0m, 0m, null);

        var securityDepositInvoice = securityDepositInvoices[0];
        var journalEntriesBySourceId = await LoadJournalEntriesForInvoiceAsync(organizationId, officeId, securityDepositInvoice);
        var (paidJournalEntryId, paidJournalEntryCode) = ResolvePaymentJournalEntryForDepositInvoice(journalEntriesBySourceId, undepositedFundsAccountId);
        var depositAmount = 0m;
        var collectedAmount = 0m;
        foreach (var sdInvoice in securityDepositInvoices)
        {
            depositAmount += CalculateSecurityDepositChargeAmount(sdInvoice, costCodeById);
            collectedAmount += CalculateSecurityDepositPaymentAllocation(sdInvoice, costCodeById).PaidAmount;
        }

        var owedAmount = CalculateReservationOutstandingOwedAmount(invoices, costCodeById);
        var roundedCollectedAmount = RoundSecurityDepositAmount(collectedAmount);
        var balanceAmount = RoundSecurityDepositAmount(Math.Max(0m, roundedCollectedAmount - owedAmount));
        return (owedAmount, balanceAmount, new SecurityDepositInvoiceInfo
        {
            DepositAmount = RoundSecurityDepositAmount(depositAmount),
            CollectedAmount = roundedCollectedAmount,
            PaidJournalEntryId = paidJournalEntryId,
            PaidJournalEntryCode = paidJournalEntryCode,
            DepositInvoiceId = securityDepositInvoice.InvoiceId,
            DepositInvoiceCode = securityDepositInvoice.InvoiceCode?.Trim() ?? string.Empty
        });
    }

    private static List<SecurityDepositDetailLine> BuildSecurityDepositChargeLines(IReadOnlyList<Invoice> securityDepositInvoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return securityDepositInvoices
            .SelectMany(invoice => (invoice.LedgerLines ?? [])
                .Where(line => line.Amount != 0m)
                .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
                .Where(line => IsRefundableSecurityDepositCharge(line, costCodeById))
                .Select(line => CreateSecurityDepositDetailLine(invoice, line, line.Amount, null, null)))
            .OrderBy(line => line.LineDate)
            .ThenBy(line => line.Description)
            .ToList();
    }

    private static List<SecurityDepositDetailLine> BuildOutstandingChargeLines(IReadOnlyList<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlySet<Guid>? ignoredPaymentIds = null)
    {
        var lines = new List<SecurityDepositDetailLine>();
        foreach (var invoice in invoices)
        {
            if (CalculateInvoiceNonSecurityDepositNetDue(invoice, costCodeById, ignoredPaymentIds) <= 0m)
                continue;

            lines.AddRange(CalculateReservationChargeLineBalances([invoice], costCodeById, ignoredPaymentIds)
                .Where(entry => entry.Remaining > 0m)
                .Where(entry => !IsRefundableSecurityDepositCharge(entry.Line, costCodeById))
                .Select(entry => CreateSecurityDepositDetailLine(entry.Invoice, entry.Line, entry.Remaining, null, null)));
        }

        return lines
            .OrderBy(line => line.LineDate)
            .ThenBy(line => line.InvoiceCode)
            .ThenBy(line => line.Description)
            .ToList();
    }

    private static SecurityDepositDetailLine CreateSecurityDepositDetailLine(Invoice invoice, LedgerLine line, decimal amount, Guid? journalEntryId, string? journalEntryCode)
    {
        return new SecurityDepositDetailLine
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceCode = invoice.InvoiceCode?.Trim() ?? string.Empty,
            LedgerLineId = line.LedgerLineId,
            LineDate = line.LedgerLineDate,
            Description = line.Description?.Trim() ?? string.Empty,
            Amount = RoundSecurityDepositAmount(amount),
            JournalEntryId = journalEntryId,
            JournalEntryCode = journalEntryCode?.Trim() ?? string.Empty
        };
    }

    private static ReservationDeparture BuildSecurityDepositDeparture(Reservation reservation, Property property, Office? office = null)
    {
        var billingArrivalDate = ResolveBillingArrivalDate(reservation);
        var billingDepartureDate = ResolveBillingDepartureDate(reservation);

        return new ReservationDeparture
        {
            ReservationId = reservation.ReservationId,
            ReservationCode = reservation.ReservationCode,
            PropertyId = reservation.PropertyId,
            PropertyCode = property.PropertyCode,
            OfficeId = reservation.OfficeId,
            OfficeName = reservation.OfficeName,
            ContactId = reservation.ContactIds.FirstOrDefault(),
            ContactName = reservation.ContactName,
            CompanyId = reservation.CompanyId,
            CompanyName = reservation.CompanyName,
            TenantName = reservation.TenantName ?? string.Empty,
            Deposit = reservation.Deposit,
            DepositType = reservation.DepositType,
            DepositReturned = reservation.DepositReturned,
            PaymentProcessed = reservation.PaymentProcessed,
            ArrivalDate = billingArrivalDate,
            DepartureDate = billingDepartureDate,
            BillingRate = reservation.BillingRate,
            BillingTypeId = (int)reservation.BillingType,
            SecurityDepositReturnDate = office != null && billingDepartureDate != default
                ? billingDepartureDate.AddDays(office.DaysToRefundDeposit)
                : default
        };
    }

    private static decimal CalculateReservationOutstandingOwedAmount(IReadOnlyList<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById)
        => RoundSecurityDepositAmount(BuildOutstandingChargeLines(invoices, costCodeById)
            .Where(line => line.InvoiceId is { } invoiceId && invoiceId != Guid.Empty)
            .Sum(line => line.Amount));

    private static List<ReservationChargeLineBalance> CalculateReservationChargeLineBalances(IReadOnlyList<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlySet<Guid>? ignoredPaymentIds = null)
    {
        var chargeEntries = invoices
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceCode)
            .SelectMany(invoice => (invoice.LedgerLines ?? [])
                .Where(line => line.Amount != 0m)
                .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
                .Select(line => new ReservationChargeLineBalance
                {
                    Invoice = invoice,
                    Line = line,
                    Remaining = line.Amount
                }))
            .OrderBy(entry => entry.Invoice.InvoiceDate)
            .ThenBy(entry => entry.Invoice.InvoiceCode)
            .ThenBy(entry => entry.Line.LineNumber)
            .ToList();

        var paymentLines = invoices
            .SelectMany(invoice => invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById))
            .Where(line => !IsIgnoredSecurityDepositPayment(line, ignoredPaymentIds))
            .OrderBy(line => line.LedgerLineDate)
            .ThenBy(line => line.LineNumber)
            .ToList();

        ApplyPaymentsToChargeLineBalances(chargeEntries, paymentLines);
        return chargeEntries;
    }

    private static void ApplyPaymentsToChargeLineBalances(IReadOnlyList<ReservationChargeLineBalance> chargeEntries, IReadOnlyList<LedgerLine> paymentLines)
    {
        foreach (var payment in paymentLines)
        {
            var paymentRemaining = payment.Amount;
            if (paymentRemaining > 0m)
            {
                for (var i = 0; i < chargeEntries.Count && paymentRemaining != 0m; i++)
                {
                    if (chargeEntries[i].Remaining <= 0m)
                        continue;

                    var apply = Math.Min(paymentRemaining, chargeEntries[i].Remaining);
                    chargeEntries[i].Remaining -= apply;
                    paymentRemaining -= apply;
                }
            }
            else if (paymentRemaining < 0m)
            {
                var toReverse = Math.Abs(paymentRemaining);
                for (var i = chargeEntries.Count - 1; i >= 0 && toReverse != 0m; i--)
                {
                    if (chargeEntries[i].Remaining >= chargeEntries[i].Line.Amount)
                        continue;

                    var applied = chargeEntries[i].Line.Amount - chargeEntries[i].Remaining;
                    if (applied <= 0m)
                        continue;

                    var reverse = Math.Min(toReverse, applied);
                    chargeEntries[i].Remaining += reverse;
                    toReverse -= reverse;
                }
            }
        }
    }

    private sealed class ReservationChargeLineBalance
    {
        public Invoice Invoice { get; init; } = null!;
        public LedgerLine Line { get; init; } = null!;
        public decimal Remaining { get; set; }
    }

    private static Dictionary<Guid, decimal> CalculateSecurityDepositPaymentAmountsByLedgerLine(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var chargeLines = invoice.LedgerLines
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .OrderBy(line => line.LineNumber)
            .ToList();

        var paymentLines = invoice.LedgerLines
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById))
            .OrderBy(line => line.LedgerLineDate)
            .ThenBy(line => line.LineNumber)
            .ToList();

        var appliedToCharges = chargeLines.Select(_ => 0m).ToArray();
        var paymentAmountsByLedgerLineId = new Dictionary<Guid, decimal>();

        foreach (var payment in paymentLines)
        {
            var paymentRemaining = payment.Amount;
            if (paymentRemaining > 0m)
            {
                for (var i = 0; i < chargeLines.Count && paymentRemaining != 0m; i++)
                {
                    var chargeRemaining = chargeLines[i].Amount - appliedToCharges[i];
                    if (chargeRemaining <= 0m)
                        continue;

                    var apply = Math.Min(paymentRemaining, chargeRemaining);
                    appliedToCharges[i] += apply;
                    paymentRemaining -= apply;

                    if (IsRefundableSecurityDepositCharge(chargeLines[i], costCodeById) && apply > 0m)
                    {
                        paymentAmountsByLedgerLineId[payment.LedgerLineId] =
                            RoundSecurityDepositAmount(paymentAmountsByLedgerLineId.GetValueOrDefault(payment.LedgerLineId) + apply);
                    }
                }
            }
            else if (paymentRemaining < 0m)
            {
                var toReverse = Math.Abs(paymentRemaining);
                for (var i = chargeLines.Count - 1; i >= 0 && toReverse != 0m; i--)
                {
                    if (appliedToCharges[i] <= 0m)
                        continue;

                    var reverse = Math.Min(toReverse, appliedToCharges[i]);
                    appliedToCharges[i] -= reverse;
                    toReverse -= reverse;

                    if (IsRefundableSecurityDepositCharge(chargeLines[i], costCodeById) && reverse > 0m)
                    {
                        paymentAmountsByLedgerLineId[payment.LedgerLineId] =
                            RoundSecurityDepositAmount(paymentAmountsByLedgerLineId.GetValueOrDefault(payment.LedgerLineId) - reverse);
                    }
                }
            }
        }

        return paymentAmountsByLedgerLineId
            .Where(kvp => kvp.Value > 0m)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static (Guid? JournalEntryId, string JournalEntryCode) ResolvePaymentJournalEntryForLedgerLine(Guid paymentLedgerLineId, IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId, int undepositedFundsAccountId)
    {
        if (!journalEntriesBySourceId.TryGetValue(paymentLedgerLineId, out var journalEntries) || journalEntries.Count == 0)
            return (null, string.Empty);

        var paymentJournalEntry = SelectSecurityDepositPaymentJournalEntry(journalEntries, undepositedFundsAccountId);
        if (paymentJournalEntry == null)
            return (null, string.Empty);

        return (paymentJournalEntry.JournalEntryId, paymentJournalEntry.JournalEntryCode?.Trim() ?? string.Empty);
    }

    private static decimal RoundSecurityDepositAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static HashSet<Guid> BuildInvoiceJournalEntrySourceIds(Invoice invoice)
    {
        var sourceIds = new HashSet<Guid>();
        if (invoice.InvoiceId != Guid.Empty)
            sourceIds.Add(invoice.InvoiceId);

        foreach (var line in invoice.LedgerLines ?? [])
        {
            if (line.LedgerLineId != Guid.Empty)
                sourceIds.Add(line.LedgerLineId);
        }

        return sourceIds;
    }

    private Task<Dictionary<Guid, List<JournalEntry>>> LoadJournalEntriesForInvoiceAsync(Guid organizationId, int officeId, Invoice invoice)
        => LoadJournalEntriesBySourceIdsAsync(organizationId, officeId, BuildInvoiceJournalEntrySourceIds(invoice));

    private async Task<Dictionary<Guid, List<JournalEntry>>> LoadJournalEntriesBySourceIdsAsync(Guid organizationId, int officeId, IEnumerable<Guid> sourceIds)
    {
        var journalEntriesBySourceId = new Dictionary<Guid, List<JournalEntry>>();
        foreach (var sourceId in sourceIds.Distinct())
        {
            if (sourceId == Guid.Empty)
                continue;

            var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                SourceId = sourceId,
                IncludeUnposted = true
            })).ToList();

            if (journalEntries.Count > 0)
                journalEntriesBySourceId[sourceId] = journalEntries;
        }

        return journalEntriesBySourceId;
    }

    private static (Guid? JournalEntryId, string JournalEntryCode) ResolvePaymentJournalEntryForDepositInvoice(IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId, int undepositedFundsAccountId)
    {
        foreach (var journalEntries in journalEntriesBySourceId.Values)
        {
            var paymentJournalEntry = SelectSecurityDepositPaymentJournalEntry(journalEntries, undepositedFundsAccountId);
            if (paymentJournalEntry != null)
                return (paymentJournalEntry.JournalEntryId, paymentJournalEntry.JournalEntryCode?.Trim() ?? string.Empty);
        }

        return (null, string.Empty);
    }

    private static JournalEntry? SelectSecurityDepositPaymentJournalEntry(IReadOnlyList<JournalEntry> journalEntries, int undepositedFundsAccountId)
    {
        if (journalEntries.Count == 0)
            return null;

        var standardPayment = journalEntries.FirstOrDefault(entry =>
            entry.JournalEntryKindId == JournalEntryKind.Payment
            && !entry.IsCashOnly
            && (entry.JournalEntryLines ?? []).Any(line => line.ChartOfAccountId == undepositedFundsAccountId && line.Debit > 0));
        if (standardPayment != null)
            return standardPayment;

        return journalEntries.FirstOrDefault(entry =>
            entry.JournalEntryKindId == JournalEntryKind.Payment && !entry.IsCashOnly);
    }

    private static List<int> ParseOfficeIdsFromAccess(string officeAccess)
        => (officeAccess ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();

    #endregion
}

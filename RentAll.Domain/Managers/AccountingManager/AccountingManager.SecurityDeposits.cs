using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
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
            EscrowAccountLabel = escrowAccountLabels.Count == 1 ? escrowAccountLabels[0] : "Escrow Security Deposit"
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
        var outstandingCharges = BuildOutstandingChargeLines(invoices, costCodeById);

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
            .ToList();

        var refundPayments = (await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices, reservation.ReservationCode))
            .Where(payment => IsSecurityDepositRefundMemo(payment.Description));
        foreach (var payment in refundPayments)
        {
            var linkedEntries = await GetJournalEntriesByPaymentIdCachedAsync(organizationId, payment.PaymentId);
            var entry = linkedEntries.FirstOrDefault(item => item.SourceTypeId == (int)SourceType.InvoicePayment) ?? linkedEntries.FirstOrDefault();
            returnPayments.Add(new SecurityDepositDetailReturnLine
            {
                JournalEntryId = entry?.JournalEntryId ?? Guid.Empty,
                JournalEntryCode = entry?.JournalEntryCode?.Trim() ?? payment.PaymentCode?.Trim() ?? string.Empty,
                TransactionDate = payment.PaymentDate,
                Memo = payment.Description?.Trim() ?? string.Empty,
                Amount = RoundSecurityDepositAmount(Math.Abs(payment.Amount))
            });
        }

        returnPayments = returnPayments
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode)
            .ToList();

        var balanceAmount = RoundSecurityDepositAmount(departure.BalanceAmount);
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

        var (_, costCodeById) = await LoadOfficeCostCodeContextAsync(organizationId, reservation.OfficeId);
        var invoices = await GetInvoicesForReservationsAsync(organizationId, reservation.OfficeId, [reservation.ReservationId]);

        var contactId = reservation.ContactIds.FirstOrDefault(id => id != Guid.Empty) is { } id && id != Guid.Empty ? id : reservation.CompanyId ?? Guid.Empty;
        var depositContext = new SecurityDepositContext
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            ReservationId = reservation.ReservationId,
            ReservationCode = reservation.ReservationCode,
            CollectedAmount = collectedAmount,
            ChartOfAccountId = chartOfAccountId,
            PaymentType = paymentType,
            PaymentDate = paymentDate,
            CurrentUser = currentUser,
            Memo = BuildSecurityDepositPaymentMemo(reservation.ReservationCode),
            LineContext = new JournalEntryLineContext(reservation.PropertyId, reservation.PropertyCode, reservation.ReservationId, reservation.ReservationCode, contactId, reservation.ContactName)
        };

        Payment? applicationPayment = null;
        Payment? refundPayment = null;
        Transfer? allocationTransfer = null;
        var chargeAmount = RoundSecurityDepositAmount(BuildOutstandingChargeLines(invoices, costCodeById).Sum(line => line.Amount));
        try
        {
            var returnInvoice = ResolveSecurityDepositReturnInvoice(invoices, costCodeById);
            applicationPayment = await CreateSecurityDepositInvoicePaymentAsync(depositContext, returnInvoice.InvoiceId, costCodeById, officeAccess);
            var refundAmount = RoundSecurityDepositAmount(amount);
            if (refundAmount != 0)
                refundPayment = await CreateSecurityDepositRefundPaymentAsync(depositContext, returnInvoice.InvoiceId, costCodeById, officeAccess, refundAmount);
            var keptAmount = RoundSecurityDepositAmount(depositContext.CollectedAmount - Math.Min(Math.Abs(refundAmount), depositContext.CollectedAmount));
            if (!IsSecurityDepositCardRefund(depositContext.PaymentType) && chargeAmount != 0m && keptAmount != 0m)
                allocationTransfer = await CreateSecurityDepositAllocationTransferAsync(depositContext, returnInvoice.InvoiceCode, refundAmount);

            await _reservationRepository.MarkDepositReturnedAsync(reservation.ReservationId, organizationId, currentUser);
        }
        catch
        {
            if (allocationTransfer != null)
                await DeleteTransferAsync(allocationTransfer.TransferId, organizationId, currentUser);
            if (refundPayment != null)
                await DeletePaymentAsync(refundPayment.PaymentId, organizationId, currentUser);
            if (applicationPayment != null)
                await DeletePaymentAsync(applicationPayment.PaymentId, organizationId, currentUser);
            throw;
        }
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

        var entries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(CultureInfo.InvariantCulture),
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = reservation.ReservationId,
            IncludeUnposted = true
        }) ?? []).ToList();
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
            await DeleteSecurityDepositAllocationTransferAsync(organizationId, reservation, currentUser);
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


    #region Deposit Application

    private static Invoice ResolveSecurityDepositReturnInvoice(IReadOnlyList<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var activeInvoices = invoices
            .Where(invoice => invoice.IsActive)
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceCode)
            .ToList();
        if (activeInvoices.Count == 0)
            throw new Exception("No invoice was found for the security deposit return");

        var invoiceWithOutstandingCharges = activeInvoices.LastOrDefault(invoice => CalculateInvoiceNonSecurityDepositNetDue(invoice, costCodeById) > 0m);
        return invoiceWithOutstandingCharges ?? activeInvoices[^1];
    }

    private async Task<Payment> CreateSecurityDepositInvoicePaymentAsync(SecurityDepositContext context, Guid invoiceId, IReadOnlyDictionary<int, CostCode> costCodeById, string officeAccess)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var (office, _) = await LoadOfficeCostCodeContextAsync(context.OrganizationId, context.OfficeId);
        var appliedCostCodeId = ResolveAppliedSecurityDepositPaymentCostCodeId(office, costCodeById);
        costCodeById.TryGetValue(appliedCostCodeId, out var appliedCostCode);
        var appliedAccountId = GetChartOfAccountIdByCostCode(
            chartOfAccounts,
            context.OfficeId,
            appliedCostCode,
            GetDefaultSecurityDepositAccount(chartOfAccounts, context.OfficeId, office, costCodeById, accountingOffice));
        var payment = new Payment
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            PaymentDate = context.PaymentDate,
            Amount = context.CollectedAmount,
            CostCodeId = appliedCostCodeId,
            Description = context.Memo,
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)PaymentType.SecurityDeposit,
            ChartOfAccountId = appliedAccountId,
            IsActive = true,
            CreatedBy = context.CurrentUser
        };

        return await ApplyInvoicePaymentAsync(payment, new List<Guid> { invoiceId }, null, officeAccess, context.CurrentUser);
    }

    private async Task<Payment> CreateSecurityDepositRefundPaymentAsync(SecurityDepositContext context, Guid invoiceId, IReadOnlyDictionary<int, CostCode> costCodeById, string officeAccess, decimal refundAmount)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var isCardRefund = IsSecurityDepositCardRefund(context.PaymentType);
        var refundAccountId = isCardRefund
            ? GetDefaultUndepositedFunds(chartOfAccounts, context.OfficeId, accountingOffice)
            : GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        if (refundAccountId <= 0)
            throw new Exception(isCardRefund
                ? "Undeposited funds is not configured for this office"
                : "Security deposit escrow is not configured for this office");

        var payment = new Payment
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            PaymentDate = context.PaymentDate,
            Amount = -RoundSecurityDepositAmount(Math.Abs(refundAmount)),
            CostCodeId = ResolveSecurityDepositPaymentCostCodeId(costCodeById),
            Description = BuildSecurityDepositRefundMemo(context.ReservationCode),
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)context.PaymentType,
            ChartOfAccountId = refundAccountId,
            IsActive = true,
            CreatedBy = context.CurrentUser
        };

        return await ApplyInvoicePaymentAsync(payment, new List<Guid> { invoiceId }, null, officeAccess, context.CurrentUser);
    }

    private static bool IsSecurityDepositCardRefund(PaymentType paymentType)
        => paymentType is PaymentType.CreditCard or PaymentType.CreditCardRefund;

    private static bool ShouldCreateSecurityDepositTransfer(PaymentType paymentType, decimal collectedAmount, decimal refundAmount)
    {
        var collected = RoundSecurityDepositAmount(collectedAmount);
        var tenantAmount = RoundSecurityDepositAmount(Math.Min(Math.Abs(refundAmount), collected));
        var businessAmount = RoundSecurityDepositAmount(collected - tenantAmount);
        if (paymentType == PaymentType.CreditCardRefund)
            return collected != 0m;

        return (paymentType == PaymentType.Check || paymentType == PaymentType.Ach) && businessAmount != 0m;
    }

    private async Task<Transfer> CreateSecurityDepositAllocationTransferAsync(SecurityDepositContext context, string invoiceCode, decimal refundAmount)
    {
        var collectedAmount = RoundSecurityDepositAmount(context.CollectedAmount);
        var tbrAmount = RoundSecurityDepositAmount(Math.Min(Math.Abs(refundAmount), collectedAmount));
        var businessAmount = RoundSecurityDepositAmount(collectedAmount - tbrAmount);
        if (collectedAmount == 0)
            throw new Exception("No security deposit amount is available to transfer");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(context.OrganizationId, context.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var businessAccountId = GetDefaultBankAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        var escrowDepositAccountId = GetDefaultEscrowDepositAccount(chartOfAccounts, context.OfficeId, accountingOffice);
        if (escrowSecurityDepositAccountId <= 0 || (businessAmount != 0 && businessAccountId <= 0))
            throw new Exception("Security deposit escrow or business bank is not configured for this office");

        var moveTenantOnTransfer = context.PaymentType == PaymentType.CreditCardRefund;
        var tbrAccountId = moveTenantOnTransfer
            ? (escrowDepositAccountId > 0 ? escrowDepositAccountId : escrowSecurityDepositAccountId)
            : context.ChartOfAccountId;
        if (moveTenantOnTransfer && tbrAmount != 0 && tbrAccountId <= 0)
            throw new Exception("Select the bank account for this security deposit refund");

        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new Exception("No invoice was found for the security deposit return");

        var description = BuildSecurityDepositTransferCreditMemo(invoiceCode);
        var splits = new List<TransferSplit>();
        if (businessAmount != 0)
            splits.Add(BuildSecurityDepositAllocationSplit(context, businessAccountId, businessAmount, description));
        if (moveTenantOnTransfer && tbrAmount != 0)
            splits.Add(BuildSecurityDepositAllocationSplit(context, tbrAccountId, tbrAmount, $"{invoiceCode.Trim()}: Transfer: Security Deposit Transfer TBR"));
        if (splits.Count == 0)
            throw new Exception("No security deposit amount is available to transfer");

        var transferCode = await _organizationManager.GenerateEntityCodeAsync(context.OrganizationId, EntityType.Transfer);
        if (string.IsNullOrWhiteSpace(transferCode))
            throw new Exception("Unable to generate transfer code");

        var transfer = new Transfer
        {
            TransferCode = transferCode,
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            TransferDate = context.PaymentDate,
            AccountingPeriod = FirstDayOfMonth(context.PaymentDate),
            Amount = RoundSecurityDepositAmount(splits.Sum(split => split.Amount)),
            Description = description,
            PropertyId = context.LineContext.PropertyId,
            BankAccountId = escrowSecurityDepositAccountId,
            IsActive = true,
            Splits = splits,
            CreatedBy = context.CurrentUser
        };
        return await CreateTransferAsync(transfer, context.CurrentUser);
    }

    private static TransferSplit BuildSecurityDepositAllocationSplit(SecurityDepositContext context, int chartOfAccountId, decimal amount, string description)
        => new()
        {
            Amount = amount,
            Description = description,
            ChartOfAccountId = chartOfAccountId,
            PropertyId = context.LineContext.PropertyId,
            PropertyCode = context.LineContext.PropertyCode,
            ReservationId = context.LineContext.ReservationId,
            ReservationCode = context.LineContext.ReservationCode,
            ContactId = context.LineContext.ContactId,
            ContactName = context.LineContext.ContactName
        };

    private async Task DeleteSecurityDepositAllocationTransferAsync(Guid organizationId, Reservation reservation, Guid currentUser)
    {
        var transfers = await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(CultureInfo.InvariantCulture),
            IncludeInactive = true
        });
        foreach (var transfer in transfers ?? [])
        {
            var matchesReservation = transfer.Splits?.Any(split => split.ReservationId == reservation.ReservationId) == true
                || IsSecurityDepositTransferForReservation(transfer.Description, reservation.ReservationCode);
            if (matchesReservation && IsSecurityDepositAllocationMemo(transfer.Description))
                await DeleteTransferAsync(transfer.TransferId, organizationId, currentUser);
        }
    }

    private static int ResolveAppliedSecurityDepositPaymentCostCodeId(Office? office, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        if (office?.SecurityDepositCcId is not int securityDepositCcId
            || securityDepositCcId <= 0
            || !costCodeById.TryGetValue(securityDepositCcId, out var securityDepositCostCode)
            || string.IsNullOrWhiteSpace(securityDepositCostCode.Code))
            throw new Exception("No Security Deposit cost code is configured for this office");

        var paymentCostCodes = costCodeById.Values
            .Where(code => code.IsActive
                && code.TransactionType == TransactionType.Payment
                && string.Equals(code.Code, securityDepositCostCode.Code, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var appliedCostCode = paymentCostCodes
            .Where(code => code.Description.Contains("Applied", StringComparison.OrdinalIgnoreCase))
            .OrderBy(code => code.CostCodeId)
            .FirstOrDefault()
            ?? paymentCostCodes
                .Where(code => !code.Description.Contains("Refund", StringComparison.OrdinalIgnoreCase))
                .OrderBy(code => code.CostCodeId)
                .FirstOrDefault();
        if (appliedCostCode == null)
            throw new Exception($"No payment cost code is configured for security deposit account {securityDepositCostCode.Code}");

        return appliedCostCode.CostCodeId;
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
        var payments = await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices, reservation.ReservationCode);
        foreach (var payment in payments)
            await DeletePaymentAsync(payment.PaymentId, organizationId, currentUser);
    }

    private async Task<List<Payment>> GetSecurityDepositApplicationPaymentsAsync(Guid organizationId, IReadOnlyList<Invoice> invoices, string reservationCode)
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
            if (payment == null)
                continue;

            var refundMemo = BuildSecurityDepositRefundMemo(reservationCode);
            if (payment.PaymentTypeId == (int)PaymentType.SecurityDeposit
                || string.Equals(payment.Description?.Trim(), refundMemo, StringComparison.OrdinalIgnoreCase))
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
        var refundPaidByReservation = await GetSecurityDepositRefundPaidAmountsByReservationAsync(organizationId, rows);
        var transferredByReservation = await GetSecurityDepositTransferAmountsByReservationAsync(organizationId, reservationIds, officeIds);
        var allocationTransferredByReservation = await GetSecurityDepositAllocationTransferredAmountsByReservationAsync(organizationId, rows);

        foreach (var row in rows)
        {
            row.OwedAmount = invoiceEnrichment.OwedByReservation.TryGetValue(row.ReservationId, out var owedAmount)
                ? owedAmount
                : 0m;
            row.BalanceAmount = invoiceEnrichment.BalanceByReservation.TryGetValue(row.ReservationId, out var balanceAmount)
                ? balanceAmount
                : 0m;
            var refundPaidAmount = refundPaidByReservation.TryGetValue(row.ReservationId, out var refundPaid) ? refundPaid : 0m;
            if (returnedByReservation.TryGetValue(row.ReservationId, out var returnedActivity))
            {
                row.ReturnedAmount = RoundSecurityDepositAmount(returnedActivity.Amount + refundPaidAmount);
                row.ReturnJournalEntryId = returnedActivity.JournalEntryId;
                row.ReturnJournalEntryCode = returnedActivity.JournalEntryCode;
            }
            else
            {
                row.ReturnedAmount = refundPaidAmount;
                row.ReturnJournalEntryId = null;
                row.ReturnJournalEntryCode = string.Empty;
            }
            var transferJournalAmount = transferredByReservation.TryGetValue(row.ReservationId, out var transferredAmount)
                ? transferredAmount
                : 0m;
            var allocationTransferAmount = allocationTransferredByReservation.TryGetValue(row.ReservationId, out var allocationTransferred)
                ? allocationTransferred
                : 0m;
            row.TransferredAmount = RoundSecurityDepositAmount(transferJournalAmount + allocationTransferAmount);
            var pendingTransferAmount = invoiceEnrichment.ToBeTransferredByReservation.TryGetValue(row.ReservationId, out var pendingTransfer)
                ? pendingTransfer
                : 0m;
            row.ToBeTransferredAmount = RoundSecurityDepositAmount(Math.Max(0m, pendingTransferAmount));

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
            ToBeTransferredByReservation = rows.Select(row => row.ReservationId).Distinct().ToDictionary(id => id, _ => 0m),
            SecurityDepositInfoByReservation = new Dictionary<Guid, SecurityDepositInvoiceInfo>()
        };

        if (rows.Count == 0)
            return enrichment;

        foreach (var row in rows)
        {
            var (owedAmount, balanceAmount, toBeTransferredAmount, securityDepositInfo) = await LoadSecurityDepositInvoiceAmountsAsync(organizationId, row.ReservationId, row.OfficeId);
            enrichment.OwedByReservation[row.ReservationId] = owedAmount;
            enrichment.BalanceByReservation[row.ReservationId] = balanceAmount;
            enrichment.ToBeTransferredByReservation[row.ReservationId] = toBeTransferredAmount;
            if (securityDepositInfo != null)
                enrichment.SecurityDepositInfoByReservation[row.ReservationId] = securityDepositInfo;
        }

        return enrichment;
    }

    #endregion

    #region Invoice Payment Allocation

    private static SecurityDepositPaymentAllocation CalculateSecurityDepositPaymentAllocation(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlySet<Guid>? ignoredPaymentIds = null)
    {
        var chargeLines = invoice.LedgerLines
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .OrderBy(line => line.LineNumber)
            .ToList();

        var paymentLines = invoice.LedgerLines
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById))
            .Where(line => !IsIgnoredSecurityDepositPayment(line, ignoredPaymentIds))
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

    private static decimal CalculateInvoiceNonSecurityDepositNetDue(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlySet<Guid>? ignoredPaymentIds = null)
    {
        // Same total the invoice editor shows for rent and fees: charges minus payments, excluding security-deposit charges.
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

   private static List<Invoice> FindSecurityDepositInvoicesForReservation(IReadOnlyList<Invoice> reservationInvoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        // Normal operation expects exactly one security-deposit invoice per reservation.
        // When more than one exists, callers sum deposit/paid/owed and keep the first invoice for JE display links.
        return reservationInvoices
            .Where(invoice => (invoice.LedgerLines ?? [])
                .Where(line => line.Amount != 0m)
                .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
                .Any(line => IsRefundableSecurityDepositCharge(line, costCodeById)))
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceCode)
            .ToList();
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

            var balance = RoundSecurityDepositAmount((lines ?? []).Sum(line => line.Credit - line.Debit));
            return (accountLabel, balance);
        }
        catch
        {
            // Report header only — skip offices without escrow security-deposit account setup.
            return (string.Empty, 0m);
        }
    }

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
                        Amount = RoundSecurityDepositAmount(group.Sum(entry =>
                        {
                            if (!accountsReceivableAccountIdByOffice.TryGetValue(entry.OfficeId, out var accountsReceivableAccountId))
                                return 0m;

                            return CalculateSecurityDepositAccountsReceivableDebit(entry, accountsReceivableAccountId);
                        })),
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

    private static decimal CalculateSecurityDepositAccountsReceivableDebit(JournalEntry journalEntry, int accountsReceivableAccountId)
    {
        if (accountsReceivableAccountId <= 0)
            return 0m;

        return (journalEntry.JournalEntryLines ?? [])
            .Where(line => line.ChartOfAccountId == accountsReceivableAccountId)
            .Sum(line => line.Debit);
    }

    private async Task<Dictionary<Guid, decimal>> GetSecurityDepositRefundPaidAmountsByReservationAsync(Guid organizationId, IList<ReservationDeparture> rows)
    {
        var paidByReservation = new Dictionary<Guid, decimal>();
        foreach (var row in rows)
        {
            var invoices = await GetInvoicesForReservationsAsync(organizationId, row.OfficeId, [row.ReservationId]);
            var payments = await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices, row.ReservationCode);
            var refundPaid = payments
                .Where(payment => IsSecurityDepositRefundMemo(payment.Description))
                .Sum(payment => Math.Abs(payment.Amount));
            paidByReservation[row.ReservationId] = RoundSecurityDepositAmount(refundPaid);
        }

        return paidByReservation;
    }

    private async Task<Dictionary<Guid, decimal>> GetSecurityDepositAllocationTransferredAmountsByReservationAsync(Guid organizationId, IList<ReservationDeparture> rows)
    {
        var transferredByReservation = new Dictionary<Guid, decimal>();
        foreach (var officeId in rows.Select(row => row.OfficeId).Where(id => id > 0).Distinct())
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            var businessAccountId = GetDefaultBankAccount(chartOfAccounts, officeId, accountingOffice);
            var transfers = await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                IncludeInactive = true
            });
            foreach (var transfer in transfers ?? [])
            {
                var splits = transfer.Splits ?? [];
                if (!IsSecurityDepositAllocationMemo(transfer.Description)
                    && !splits.Any(split => IsSecurityDepositAllocationMemo(split.Description)))
                    continue;

                var reservationId = splits.Select(split => split.ReservationId).FirstOrDefault(id => id is { } value && value != Guid.Empty);
                var matchedRow = reservationId is { } splitReservationId && splitReservationId != Guid.Empty
                    ? rows.FirstOrDefault(row => row.ReservationId == splitReservationId)
                    : rows.FirstOrDefault(row => IsSecurityDepositTransferForReservation(transfer.Description, row.ReservationCode)
                        || splits.Any(split => IsSecurityDepositTransferForReservation(split.Description, row.ReservationCode)));
                if (matchedRow == null)
                    continue;

                var businessAmount = splits.Any(split => IsSecurityDepositAllocationTbrMemo(split.Description))
                    ? splits.Where(split => !IsSecurityDepositAllocationTbrMemo(split.Description)).Sum(split => split.Amount)
                    : businessAccountId > 0
                        ? splits.Where(split => split.ChartOfAccountId == businessAccountId).Sum(split => split.Amount)
                        : 0m;
                transferredByReservation[matchedRow.ReservationId] = RoundSecurityDepositAmount(
                    (transferredByReservation.TryGetValue(matchedRow.ReservationId, out var current) ? current : 0m) + businessAmount);
            }
        }

        return transferredByReservation;
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

    #region Helper Functions

    private async Task<decimal> GetSecurityDepositCollectedAmountAsync(Guid organizationId, Guid reservationId, int officeId)
    {
        var (_, _, _, securityDepositInfo) = await LoadSecurityDepositInvoiceAmountsAsync(organizationId, reservationId, officeId);
        return securityDepositInfo?.CollectedAmount ?? 0m;
    }

    private async Task<(decimal OwedAmount, decimal BalanceAmount, decimal ToBeTransferredAmount, SecurityDepositInvoiceInfo? SecurityDepositInfo)> LoadSecurityDepositInvoiceAmountsAsync(Guid organizationId, Guid reservationId, int officeId)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, officeId);
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice);
        var invoices = await GetInvoicesForReservationsAsync(organizationId, officeId, [reservationId]);
        var securityDepositInvoices = FindSecurityDepositInvoicesForReservation(invoices, costCodeById);
        if (securityDepositInvoices.Count == 0)
            return (0m, 0m, 0m, null);

        var securityDepositInvoice = securityDepositInvoices[0];
        var journalEntriesBySourceId = await LoadJournalEntriesForInvoiceAsync(organizationId, officeId, securityDepositInvoice);
        var (paidJournalEntryId, paidJournalEntryCode) = ResolvePaymentJournalEntryForDepositInvoice(journalEntriesBySourceId, undepositedFundsAccountId);
        var reservationCode = invoices.Select(invoice => invoice.ReservationCode).FirstOrDefault(code => !string.IsNullOrWhiteSpace(code)) ?? string.Empty;
        var applicationPayments = await GetSecurityDepositApplicationPaymentsAsync(organizationId, invoices, reservationCode);
        var ignoredPaymentIds = applicationPayments
            .Where(payment => IsSecurityDepositRefundMemo(payment.Description)
                || IsSecurityDepositPaymentMemo(payment.Description, reservationCode))
            .Select(payment => payment.PaymentId)
            .ToHashSet();
        var rePaymentAmount = RoundSecurityDepositAmount(applicationPayments
            .Where(payment => IsSecurityDepositPaymentMemo(payment.Description, reservationCode))
            .Sum(payment => payment.Amount));
        var depositAmount = 0m;
        var collectedAmount = 0m;
        foreach (var sdInvoice in securityDepositInvoices)
        {
            depositAmount += CalculateSecurityDepositChargeAmount(sdInvoice, costCodeById);
            collectedAmount += CalculateSecurityDepositPaymentAllocation(sdInvoice, costCodeById, ignoredPaymentIds).PaidAmount;
        }

        var heldOwedAmount = RoundSecurityDepositAmount(BuildOutstandingChargeLines(invoices, costCodeById, ignoredPaymentIds)
            .Where(line => line.InvoiceId is { } heldInvoiceId && heldInvoiceId != Guid.Empty)
            .Sum(line => line.Amount));
        var owedAmount = RoundSecurityDepositAmount(BuildOutstandingChargeLines(invoices, costCodeById)
            .Where(line => line.InvoiceId is { } invoiceId && invoiceId != Guid.Empty)
            .Sum(line => line.Amount));
        var roundedCollectedAmount = RoundSecurityDepositAmount(collectedAmount);
        var balanceAmount = RoundSecurityDepositAmount(Math.Max(0m, roundedCollectedAmount - heldOwedAmount));
        var toBeTransferredAmount = RoundSecurityDepositAmount(Math.Max(0m, rePaymentAmount - balanceAmount));
        return (owedAmount, balanceAmount, toBeTransferredAmount, new SecurityDepositInvoiceInfo
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

    #region Helper Classes

    private class SecurityDepositContext
    {
        public Guid OrganizationId { get; init; }
        public int OfficeId { get; init; }
        public Guid ReservationId { get; init; }
        public string ReservationCode { get; init; } = string.Empty;
        public decimal CollectedAmount { get; init; }
        public int ChartOfAccountId { get; init; }
        public PaymentType PaymentType { get; init; }
        public DateOnly PaymentDate { get; init; }
        public Guid CurrentUser { get; init; }
        public string Memo { get; init; } = string.Empty;
        public JournalEntryLineContext LineContext { get; init; }
    }

    private class ReservationInvoiceEnrichment
    {
        public Dictionary<Guid, decimal> OwedByReservation { get; init; } = new();
        public Dictionary<Guid, decimal> BalanceByReservation { get; init; } = new();
        public Dictionary<Guid, decimal> ToBeTransferredByReservation { get; init; } = new();
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

    private class SecurityDepositPaymentAllocation
    {
        public decimal PaidAmount { get; init; }
    }

    private sealed class SecurityDepositReturnActivity
    {
        public decimal Amount { get; init; }
        public Guid? JournalEntryId { get; init; }
        public string JournalEntryCode { get; init; } = string.Empty;
    }

    private sealed class ReservationChargeLineBalance
    {
        public Invoice Invoice { get; init; } = null!;
        public LedgerLine Line { get; init; } = null!;
        public decimal Remaining { get; set; }
    }

    #endregion

    #region Helpber Memos

    private static string BuildSecurityDepositPaymentMemo(string reservationCode)
        => $"{reservationCode.Trim()}: Security Deposit Re-Payment";

    private static string BuildSecurityDepositRefundMemo(string reservationCode)
        => $"{reservationCode.Trim()}: Security Deposit Refund";

    private static string BuildSecurityDepositAllocationMemo(string reservationCode)
        => $"{reservationCode.Trim()}: Security Deposit Transfer";

    private static string BuildSecurityDepositTransferCreditMemo(string invoiceCode)
        => $"{invoiceCode.Trim()}: Transfer: Security Deposit Transfer";

    private static string BuildSecurityDepositTransferDebitMemo(string invoiceCode)
        => $"{invoiceCode.Trim()}: Transfer: Outstanding Charges";

    private static string BuildSecurityDepositTransferTbrMemo(string invoiceCode)
        => $"{invoiceCode.Trim()}: Transfer: To Be Returned";

    private static string SecurityDepositTransferInvoicePrefix(string? description)
    {
        var value = (description ?? string.Empty).Trim();
        const string marker = ": Transfer:";
        var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex > 0)
            return value[..markerIndex].Trim();

        var colon = value.IndexOf(':');
        return colon > 0 ? value[..colon].Trim() : value;
    }

    private static string BuildSecurityDepositAllocationTbrMemo(string reservationCode)
        => $"{reservationCode.Trim()}: Security Deposit Transfer TBR";

    private static bool IsSecurityDepositPaymentMemo(string? description, string reservationCode)
    {
        var value = (description ?? string.Empty).Trim();
        var code = reservationCode.Trim();
        return string.Equals(value, BuildSecurityDepositPaymentMemo(code), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, $"{code}: Security Deposit Payment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, $"{code}: Security Deposit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSecurityDepositRefundMemo(string? description)
        => string.Equals((description ?? string.Empty).Trim(), BuildSecurityDepositRefundMemo(ExtractReservationCode(description)), StringComparison.OrdinalIgnoreCase)
            || (description ?? string.Empty).Contains(": Security Deposit Refund", StringComparison.OrdinalIgnoreCase);

    private static bool IsSecurityDepositAllocationMemo(string? description)
    {
        var value = description ?? string.Empty;
        return value.Contains(": Security Deposit Allocation", StringComparison.OrdinalIgnoreCase)
            || value.Contains(": Security Deposit Transfer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSecurityDepositAllocationTbrMemo(string? description)
    {
        var value = description ?? string.Empty;
        return value.Contains(": Security Deposit Allocation TBR", StringComparison.OrdinalIgnoreCase)
            || value.Contains(": Security Deposit Transfer TBR", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSecurityDepositTransferForReservation(string? description, string reservationCode)
    {
        var code = reservationCode.Trim();
        var value = (description ?? string.Empty).Trim();
        return string.Equals(value, $"{code}: Security Deposit Transfer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, $"{code}: Security Deposit Allocation", StringComparison.OrdinalIgnoreCase)
            || (value.Contains(code, StringComparison.OrdinalIgnoreCase)
                && value.Contains(": Transfer: Security Deposit Transfer", StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtractReservationCode(string? description)
    {
        var value = (description ?? string.Empty).Trim();
        var separator = value.IndexOf(':');
        return separator > 0 ? value[..separator].Trim() : value;
    }

    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Globalization;
using System.Text;

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

        var securityDepositCharges = BuildSecurityDepositChargeLines(securityDepositInvoices, costCodeById);
        var outstandingCharges = departure.OwedAmount > 0
            ? BuildOutstandingChargeLines(invoices, costCodeById)
            : [];

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
            IncludeVoided = false,
            IncludeUnposted = true
        });

        var returnPayments = (returnJournalEntries ?? [])
            .Where(entry => !IsSecurityDepositTransferJournalEntry(entry))
            .Select(entry => new SecurityDepositDetailReturnLine
            {
                JournalEntryId = entry.JournalEntryId,
                JournalEntryCode = entry.JournalEntryCode?.Trim() ?? string.Empty,
                TransactionDate = entry.TransactionDate,
                Memo = entry.Memo?.Trim() ?? string.Empty,
                Amount = RoundSecurityDepositAmount(CalculateSecurityDepositReturnJournalEntryAmount(entry))
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
        _ = paymentType;
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

        ValidateSecurityDepositReturnReservation(reservationId, reservation, description);

        if (reservation.DepositReturned)
            throw new Exception("Security deposit has already been returned");

        if (reservation.DepositType != DepositType.Deposit)
            throw new Exception("Reservation does not have a security deposit");

        if (amount == 0)
            throw new Exception("Payment amount cannot be zero");

        if (chartOfAccountId <= 0)
            throw new Exception("Chart of account is required for security deposit return");

        if (paymentDate == default)
            throw new Exception("Payment date is required for security deposit return");

        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, organizationId)
            ?? throw new Exception("Property not found");

        var departure = new ReservationDeparture
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
            DepositReturned = reservation.DepositReturned
        };

        var collectedAmount = await GetSecurityDepositCollectedAmountAsync(organizationId, departure);
        if (collectedAmount <= 0)
            throw new Exception("No security deposit has been collected for this reservation");

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
        {
            await _reservationRepository.MarkDepositReturnedAsync(reservation.ReservationId, organizationId, currentUser);
            reservation.DepositReturned = true;
            return reservation;
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, reservation.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, reservation.OfficeId, accountingOffice);
        var lineContext = await ResolveReservationJournalEntryLineContextAsync(organizationId, departure, reservation);
        var memo = BuildSecurityDepositReturnMemo(reservation.ReservationCode, description);
        var journalEntry = BuildJournalEntryForSecurityDepositReturn(
            reservation,
            lineContext,
            escrowSecurityDepositAccountId,
            chartOfAccountId,
            memo,
            amount,
            paymentDate,
            currentUser);

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);

        await _reservationRepository.MarkDepositReturnedAsync(reservation.ReservationId, organizationId, currentUser);
        reservation.DepositReturned = true;

        return reservation;
    }

    public async Task<Reservation> ApplySecurityDepositTransferAsync(Guid reservationId, Guid organizationId, string officeAccess, int chartOfAccountId, string description, decimal amount, DateOnly paymentDate, PaymentType paymentType, Guid currentUser)
    {
        _ = officeAccess;
        _ = paymentType;
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

        if (reservation.DepositType != DepositType.Deposit)
            throw new Exception("Reservation does not have a security deposit");

        if (amount == 0)
            throw new Exception("Transfer amount cannot be zero");

        if (chartOfAccountId <= 0)
            throw new Exception("Business bank account is required for security deposit transfer");

        if (paymentDate == default)
            throw new Exception("Transfer date is required for security deposit transfer");

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return reservation;

        var property = await _propertyRepository.GetPropertyByIdAsync(reservation.PropertyId, organizationId)
            ?? throw new Exception("Property not found");

        var departure = new ReservationDeparture
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
            TenantName = reservation.TenantName ?? string.Empty
        };

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, reservation.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, reservation.OfficeId, accountingOffice);
        var lineContext = await ResolveReservationJournalEntryLineContextAsync(organizationId, departure, reservation);
        var memo = BuildSecurityDepositTransferMemo(reservation.ReservationCode, description);
        var normalizedAmount = RoundSecurityDepositAmount(Math.Abs(amount));
        var journalEntry = BuildJournalEntryForSecurityDepositTransfer(
            reservation,
            lineContext,
            escrowSecurityDepositAccountId,
            chartOfAccountId,
            memo,
            normalizedAmount,
            paymentDate,
            currentUser);

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);

        return reservation;
    }

    #endregion

    #region Journal Entry

    private JournalEntry BuildJournalEntryForSecurityDepositReturn(
        Reservation reservation,
        JournalEntryLineContext lineContext,
        int escrowSecurityDepositAccountId,
        int chartOfAccountId,
        string memo,
        decimal amount,
        DateOnly paymentDate,
        Guid currentUser)
    {
        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-RETURN-JE-ACCOUNTS
        // Line 1 — Escrow Security Deposit: Debit liability / Credit 0 (positive return) or reverse for negative.
        // Line 2 — Business bank: Credit bank / Debit 0 (positive return) or reverse for negative.
        // END SECURITY-DEPOSIT-RETURN-JE-ACCOUNTS

        var liabilityLine = new JournalEntryLine
        {
            ChartOfAccountId = escrowSecurityDepositAccountId,
            Debit = amount > 0 ? amount : 0,
            Credit = amount < 0 ? Math.Abs(amount) : 0,
            Memo = memo,
            CreatedBy = currentUser
        };
        ApplyJournalEntryLineContext(liabilityLine, lineContext);

        var bankLine = new JournalEntryLine
        {
            ChartOfAccountId = chartOfAccountId,
            Debit = amount < 0 ? Math.Abs(amount) : 0,
            Credit = amount > 0 ? amount : 0,
            Memo = memo,
            CreatedBy = currentUser
        };
        ApplyJournalEntryLineContext(bankLine, lineContext);

        return new JournalEntry
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            TransactionDate = paymentDate,
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = reservation.ReservationId,
            SourceCode = reservation.ReservationCode,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { liabilityLine, bankLine },
            CreatedBy = currentUser
        };
    }

    private JournalEntry BuildJournalEntryForSecurityDepositTransfer(
        Reservation reservation,
        JournalEntryLineContext lineContext,
        int escrowSecurityDepositAccountId,
        int chartOfAccountId,
        string memo,
        decimal amount,
        DateOnly paymentDate,
        Guid currentUser)
    {
        // AGENT-NOTE: DO NOT TOUCH.
        // SECURITY-DEPOSIT-TRANSFER-JE-ACCOUNTS
        // Line 1 — Business bank: Debit bank / Credit 0
        // Line 2 — Escrow Security Deposit: Debit 0 / Credit liability
        // END SECURITY-DEPOSIT-TRANSFER-JE-ACCOUNTS

        var bankLine = new JournalEntryLine
        {
            ChartOfAccountId = chartOfAccountId,
            Debit = amount,
            Credit = 0,
            Memo = memo,
            CreatedBy = currentUser
        };
        ApplyJournalEntryLineContext(bankLine, lineContext);

        var escrowLine = new JournalEntryLine
        {
            ChartOfAccountId = escrowSecurityDepositAccountId,
            Debit = 0,
            Credit = amount,
            Memo = memo,
            CreatedBy = currentUser
        };
        ApplyJournalEntryLineContext(escrowLine, lineContext);

        return new JournalEntry
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            TransactionDate = paymentDate,
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = reservation.ReservationId,
            SourceCode = reservation.ReservationCode,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { bankLine, escrowLine },
            CreatedBy = currentUser
        };
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
            row.ReturnedAmount = returnedByReservation.TryGetValue(row.ReservationId, out var returnedAmount)
                ? returnedAmount
                : 0m;
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
            var reservationId = row.ReservationId;
            var officeId = row.OfficeId;
            var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, officeId);
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice);

            var invoices = await GetInvoicesForReservationsAsync(organizationId, officeId, [reservationId]);
            var securityDepositInvoices = FindSecurityDepositInvoicesForReservation(invoices, costCodeById);
            if (securityDepositInvoices.Count == 0)
                continue;

            // In practice, this should not happen
            if (securityDepositInvoices.Count > 1)
                await LogMultipleSecurityDepositInvoicesAsync(organizationId, officeId, reservationId, securityDepositInvoices);

            var securityDepositInvoice = securityDepositInvoices[0];
            var journalEntriesBySourceId = await LoadJournalEntriesForInvoiceAsync(organizationId, officeId, securityDepositInvoice);
            var securityDepositPaymentAllocation = CalculateSecurityDepositPaymentAllocation(securityDepositInvoice, costCodeById);
            var (paidJournalEntryId, paidJournalEntryCode) = ResolvePaymentJournalEntryForDepositInvoice(securityDepositPaymentAllocation, journalEntriesBySourceId, undepositedFundsAccountId);

            var depositAmount = 0m;
            var collectedAmount = 0m;
            foreach (var sdInvoice in securityDepositInvoices)
            {
                depositAmount += CalculateSecurityDepositChargeAmount(sdInvoice, costCodeById);
                collectedAmount += CalculateSecurityDepositPaymentAllocation(sdInvoice, costCodeById).PaidAmount;
            }

            var chargeTotal = CalculateReservationInvoiceChargeTotal(invoices, costCodeById);
            var paymentTotal = CalculateReservationInvoicePaymentTotal(invoices, costCodeById);
            var unpaidBalance = RoundSecurityDepositAmount(chargeTotal - paymentTotal);
            var owedAmount = RoundSecurityDepositAmount(Math.Max(0m, unpaidBalance));
            var roundedCollectedAmount = RoundSecurityDepositAmount(collectedAmount);
            var balanceAmount = RoundSecurityDepositAmount(Math.Max(0m, roundedCollectedAmount - owedAmount));

            await LogSecurityDepositEnrichmentDebugAsync(
                organizationId,
                row,
                securityDepositInvoice,
                costCodeById,
                journalEntriesBySourceId,
                securityDepositPaymentAllocation,
                paidJournalEntryId,
                paidJournalEntryCode,
                RoundSecurityDepositAmount(depositAmount),
                roundedCollectedAmount,
                owedAmount,
                balanceAmount,
                chargeTotal,
                paymentTotal);

            enrichment.OwedByReservation[reservationId] = owedAmount;
            enrichment.BalanceByReservation[reservationId] = balanceAmount;
            enrichment.SecurityDepositInfoByReservation[reservationId] = new SecurityDepositInvoiceInfo
            {
                DepositAmount = RoundSecurityDepositAmount(depositAmount),
                CollectedAmount = RoundSecurityDepositAmount(collectedAmount),
                PaidJournalEntryId = paidJournalEntryId,
                PaidJournalEntryCode = paidJournalEntryCode,
                DepositInvoiceId = securityDepositInvoice.InvoiceId,
                DepositInvoiceCode = securityDepositInvoice.InvoiceCode?.Trim() ?? string.Empty
            };
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
        public IReadOnlyList<Guid> SecurityDepositPaymentLedgerLineIds { get; init; } = [];
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
        var securityDepositPaymentLedgerLineIds = new List<Guid>();

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

                    costCodeById.TryGetValue(chargeLines[i].CostCodeId, out var costCode);
                    if (costCode?.TransactionType == TransactionType.SecurityDeposit && apply > 0m)
                    {
                        securityDepositPaid += apply;
                        securityDepositPaymentLedgerLineIds.Add(payment.LedgerLineId);
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

                    costCodeById.TryGetValue(chargeLines[i].CostCodeId, out var costCode);
                    var reverse = Math.Min(toReverse, appliedToCharges[i]);
                    appliedToCharges[i] -= reverse;
                    toReverse -= reverse;

                    if (costCode?.TransactionType == TransactionType.SecurityDeposit && reverse > 0m)
                    {
                        securityDepositPaid -= reverse;
                        securityDepositPaymentLedgerLineIds.Add(payment.LedgerLineId);
                    }
                }
            }
        }

        return new SecurityDepositPaymentAllocation
        {
            PaidAmount = Math.Max(0m, securityDepositPaid),
            SecurityDepositPaymentLedgerLineIds = securityDepositPaid > 0m ? securityDepositPaymentLedgerLineIds : []
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

        var reservationIdSet = reservationIds.ToHashSet();

        return (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
            IncludeInactive = true,
            IncludePaid = true
        }))
        .Where(invoice => invoice.ReservationId.HasValue && reservationIdSet.Contains(invoice.ReservationId.Value))
        .ToList();
    }

    private static decimal CalculateReservationInvoiceChargeTotal(IEnumerable<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return RoundSecurityDepositAmount((invoices ?? [])
            .SelectMany(invoice => invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Sum(line => line.Amount));
    }

    private static decimal CalculateReservationInvoicePaymentTotal(IEnumerable<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return RoundSecurityDepositAmount((invoices ?? [])
            .SelectMany(invoice => invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById))
            .Sum(line => line.Amount));
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

    private async Task LogMultipleSecurityDepositInvoicesAsync(Guid organizationId, int officeId, Guid reservationId, IReadOnlyList<Invoice> securityDepositInvoices)
    {
        var reservationCode = securityDepositInvoices
            .Select(invoice => invoice.ReservationCode?.Trim())
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code))
            ?? reservationId.ToString();
        var invoiceCodes = string.Join(", ", securityDepositInvoices.Select(invoice => invoice.InvoiceCode?.Trim() ?? invoice.InvoiceId.ToString()));

        await LogAccountingLogAsync(new AccountingLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = securityDepositInvoices[0].PropertyId,
            Message = $"Security deposit enrichment [{reservationCode}]: found {securityDepositInvoices.Count} security-deposit invoice(s) ({invoiceCodes}); expected one. Deposit and paid amounts will be summed."
        });
    }

    private async Task LogSecurityDepositEnrichmentDebugAsync(Guid organizationId, ReservationDeparture row, Invoice securityDepositInvoice, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId, SecurityDepositPaymentAllocation paymentAllocation, Guid? selectedPaidJournalEntryId, string selectedPaidJournalEntryCode, decimal depositAmount, decimal collectedAmount, decimal owedAmount, decimal balanceAmount, decimal chargeTotal, decimal paymentTotal)
    {
        var reservationCode = row.ReservationCode?.Trim() ?? row.ReservationId.ToString();
        var invoiceCode = securityDepositInvoice.InvoiceCode?.Trim() ?? securityDepositInvoice.InvoiceId.ToString();
        var sourceIds = BuildInvoiceJournalEntrySourceIds(securityDepositInvoice);
        var headerPrefix = $"Security deposit debug [{reservationCode}] invoice {invoiceCode}";

        var chargeLineDetails = (securityDepositInvoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode?.TransactionType == TransactionType.SecurityDeposit;
            })
            .Select(line => $"LedgerLineId={line.LedgerLineId} Amount={line.Amount} Date={line.LedgerLineDate:yyyy-MM-dd} Desc={line.Description}")
            .ToList();

        await LogSecurityDepositDebugMessagesAsync(
            organizationId,
            row.OfficeId,
            securityDepositInvoice.PropertyId,
            headerPrefix,
            [
                $"InvoiceId={securityDepositInvoice.InvoiceId}; SourceIds=[{string.Join(", ", sourceIds)}]; SecurityDepositChargeLines=[{string.Join(" | ", chargeLineDetails)}]",
                BuildSecurityDepositJournalEntryDebugSection("Invoice-source journal entries", journalEntriesBySourceId),
                $"PaymentAllocation PaidAmount={paymentAllocation.PaidAmount}; SecurityDepositPaymentLedgerLineIds=[{string.Join(", ", paymentAllocation.SecurityDepositPaymentLedgerLineIds)}]",
                BuildSecurityDepositPaymentJournalEntryDebugSection(paymentAllocation, journalEntriesBySourceId),
                $"EnrichmentResult Deposit={depositAmount}; Collected={collectedAmount}; ChargeTotal={chargeTotal}; PaymentTotal={paymentTotal}; Owed={owedAmount}; Balance={balanceAmount}; SelectedPaidJournalEntryId={selectedPaidJournalEntryId}; SelectedPaidJournalEntryCode={selectedPaidJournalEntryCode}"
            ]);
    }

    private async Task LogSecurityDepositDebugMessagesAsync(Guid organizationId, int officeId, Guid? propertyId, string headerPrefix, IReadOnlyList<string> sections)
    {
        for (var index = 0; index < sections.Count; index++)
        {
            await LogAccountingLogAsync(new AccountingLog
            {
                OrganizationId = organizationId,
                OfficeId = officeId,
                PropertyId = propertyId,
                Message = $"{headerPrefix} ({index + 1}/{sections.Count}): {sections[index]}"
            });
        }
    }

    private static string BuildSecurityDepositJournalEntryDebugSection(string title, IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId)
    {
        if (journalEntriesBySourceId.Count == 0)
            return $"{title}: none";

        var builder = new StringBuilder(title).Append(':');
        foreach (var (sourceId, journalEntries) in journalEntriesBySourceId.OrderBy(pair => pair.Key))
        {
            builder.Append($" SourceId={sourceId} -> ");
            builder.Append(string.Join(" || ", journalEntries.Select(FormatSecurityDepositJournalEntryDebug)));
        }

        return builder.ToString();
    }

    private static string BuildSecurityDepositPaymentJournalEntryDebugSection(SecurityDepositPaymentAllocation paymentAllocation, IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId)
    {
        if (paymentAllocation.SecurityDepositPaymentLedgerLineIds.Count == 0)
            return "Payment journal entries: none (no security-deposit payment ledger lines)";

        var builder = new StringBuilder("Payment journal entries:");
        foreach (var paymentLedgerLineId in paymentAllocation.SecurityDepositPaymentLedgerLineIds)
        {
            builder.Append($" PaymentLedgerLineId={paymentLedgerLineId} -> ");
            if (!journalEntriesBySourceId.TryGetValue(paymentLedgerLineId, out var journalEntries) || journalEntries.Count == 0)
            {
                builder.Append("none");
                continue;
            }

            builder.Append(string.Join(" || ", journalEntries.Select(FormatSecurityDepositJournalEntryDebug)));
        }

        return builder.ToString();
    }

    private static string FormatSecurityDepositJournalEntryDebug(JournalEntry journalEntry)
    {
        var sourceType = journalEntry.SourceTypeId.HasValue
            ? ((SourceType)journalEntry.SourceTypeId.Value).ToString()
            : "null";
        var lineDetails = (journalEntry.JournalEntryLines ?? [])
            .Select(line => $"LineId={line.JournalEntryLineId} Acct={line.ChartOfAccountId} CostCode={line.CostCodeId} Dr={line.Debit} Cr={line.Credit} Memo={line.Memo}")
            .ToList();

        return $"JE={journalEntry.JournalEntryCode} JournalEntryId={journalEntry.JournalEntryId} SourceType={sourceType} SourceId={journalEntry.SourceId} Lines=[{string.Join("; ", lineDetails)}]";
    }

    private static bool InvoiceHasSecurityDepositChargeLine(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return (invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Any(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode?.TransactionType == TransactionType.SecurityDeposit;
            });
    }

    private static decimal CalculateSecurityDepositChargeAmount(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return RoundSecurityDepositAmount((invoice.LedgerLines ?? [])
            .Where(line => line.Amount != 0m)
            .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
            .Where(line =>
            {
                costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                return costCode?.TransactionType == TransactionType.SecurityDeposit;
            })
            .Sum(line => line.Amount));
    }

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
                IncludeVoided = false,
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

    private async Task<Dictionary<Guid, decimal>> GetSecurityDepositReturnPaidAmountsByReservationAsync(Guid organizationId, IReadOnlyCollection<Guid> reservationIds, IReadOnlyCollection<int> officeIds)
    {
        if (reservationIds.Count == 0 || officeIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        var reservationIdSet = reservationIds.ToHashSet();
        var entries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = string.Join(',', officeIds),
            SourceTypeId = (int)SourceType.SecurityDeposit,
            IncludeVoided = false,
            IncludeUnposted = true
        });

        return (entries ?? [])
            .Where(entry => entry.SourceId.HasValue && reservationIdSet.Contains(entry.SourceId.Value))
            .Where(entry => !IsSecurityDepositTransferJournalEntry(entry))
            .GroupBy(entry => entry.SourceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => RoundSecurityDepositAmount(group.Sum(CalculateSecurityDepositReturnJournalEntryAmount)));
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
            IncludeVoided = false,
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

    private static bool IsSecurityDepositTransferJournalEntry(JournalEntry journalEntry)
    {
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

        if (!string.Equals(descriptionReservationCode, reservation.ReservationCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Security deposit return description reservation {descriptionReservationCode} does not match selected reservation {reservation.ReservationCode}.");
    }

    private static string BuildSecurityDepositReturnMemo(string reservationCode, string description)
    {
        var normalizedReservationCode = reservationCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReservationCode))
            return string.IsNullOrWhiteSpace(description) ? "Security Deposit Return" : description.Trim();

        var defaultMemo = $"{normalizedReservationCode}: Security Deposit Return";
        if (string.IsNullOrWhiteSpace(description))
            return defaultMemo;

        var descriptionReservationCode = ExtractReservationCodeFromReturnDescription(description);
        if (descriptionReservationCode != null
            && string.Equals(descriptionReservationCode, normalizedReservationCode, StringComparison.OrdinalIgnoreCase))
            return description.Trim();

        return defaultMemo;
    }

    private static string BuildSecurityDepositTransferMemo(string reservationCode, string description)
    {
        var normalizedReservationCode = reservationCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReservationCode))
            return string.IsNullOrWhiteSpace(description) ? "Security Deposit Transfer" : description.Trim();

        var defaultMemo = $"{normalizedReservationCode}: Security Deposit Transfer";
        if (string.IsNullOrWhiteSpace(description))
            return defaultMemo;

        var descriptionReservationCode = ExtractReservationCodeFromReturnDescription(description);
        if (descriptionReservationCode != null
            && string.Equals(descriptionReservationCode, normalizedReservationCode, StringComparison.OrdinalIgnoreCase))
            return description.Trim();

        return defaultMemo;
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

    private async Task<decimal> GetSecurityDepositCollectedAmountAsync(Guid organizationId, ReservationDeparture departure)
    {
        var invoiceEnrichment = await GetReservationInvoiceEnrichmentAsync(organizationId, [departure]);
        return invoiceEnrichment.SecurityDepositInfoByReservation.TryGetValue(departure.ReservationId, out var securityDepositInfo)
            ? RoundSecurityDepositAmount(securityDepositInfo.CollectedAmount)
            : 0m;
    }

    private static List<SecurityDepositDetailLine> BuildSecurityDepositChargeLines(IReadOnlyList<Invoice> securityDepositInvoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return securityDepositInvoices
            .SelectMany(invoice => (invoice.LedgerLines ?? [])
                .Where(line => line.Amount != 0m)
                .Where(line => IsInvoiceChargeLedgerLine(line, costCodeById))
                .Where(line =>
                {
                    costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                    return costCode?.TransactionType == TransactionType.SecurityDeposit;
                })
                .Select(line => CreateSecurityDepositDetailLine(invoice, line, line.Amount, null, null)))
            .OrderBy(line => line.LineDate)
            .ThenBy(line => line.Description)
            .ToList();
    }

    private static List<SecurityDepositDetailLine> BuildOutstandingChargeLines(IReadOnlyList<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return CalculateReservationChargeLineBalances(invoices, costCodeById)
            .Where(entry => entry.Remaining > 0m)
            .Where(entry =>
            {
                costCodeById.TryGetValue(entry.Line.CostCodeId, out var costCode);
                return costCode?.TransactionType != TransactionType.SecurityDeposit;
            })
            .Select(entry => CreateSecurityDepositDetailLine(entry.Invoice, entry.Line, entry.Remaining, null, null))
            .OrderBy(line => line.LineDate)
            .ThenBy(line => line.InvoiceCode)
            .ThenBy(line => line.Description)
            .ToList();
    }

    private static SecurityDepositDetailLine CreateSecurityDepositDetailLine(
        Invoice invoice,
        LedgerLine line,
        decimal amount,
        Guid? journalEntryId,
        string? journalEntryCode)
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
            ArrivalDate = reservation.ArrivalDate,
            DepartureDate = reservation.DepartureDate,
            BillingRate = reservation.BillingRate,
            BillingTypeId = (int)reservation.BillingType,
            SecurityDepositReturnDate = office != null && reservation.DepartureDate != default
                ? reservation.DepartureDate.AddDays(office.DaysToRefundDeposit)
                : default
        };
    }

    private sealed class ReservationChargeLineBalance
    {
        public Invoice Invoice { get; init; } = null!;
        public LedgerLine Line { get; init; } = null!;
        public decimal Remaining { get; set; }
    }

    private static List<ReservationChargeLineBalance> CalculateReservationChargeLineBalances(
        IReadOnlyList<Invoice> invoices,
        IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var chargeEntries = invoices
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
            .ThenBy(entry => entry.Line.LineNumber)
            .ToList();

        var paymentLines = invoices
            .SelectMany(invoice => (invoice.LedgerLines ?? [])
                .Where(line => line.Amount != 0m)
                .Where(line => IsInvoicePaymentLedgerLine(line, costCodeById)))
            .OrderBy(line => line.LedgerLineDate)
            .ThenBy(line => line.LineNumber)
            .ToList();

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

        return chargeEntries;
    }

    private static Dictionary<Guid, decimal> CalculateSecurityDepositPaymentAmountsByLedgerLine(
        Invoice invoice,
        IReadOnlyDictionary<int, CostCode> costCodeById)
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

                    costCodeById.TryGetValue(chargeLines[i].CostCodeId, out var costCode);
                    if (costCode?.TransactionType == TransactionType.SecurityDeposit && apply > 0m)
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

                    costCodeById.TryGetValue(chargeLines[i].CostCodeId, out var costCode);
                    var reverse = Math.Min(toReverse, appliedToCharges[i]);
                    appliedToCharges[i] -= reverse;
                    toReverse -= reverse;

                    if (costCode?.TransactionType == TransactionType.SecurityDeposit && reverse > 0m)
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

    private static (Guid? JournalEntryId, string JournalEntryCode) ResolvePaymentJournalEntryForLedgerLine(
        Guid paymentLedgerLineId,
        IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId,
        int undepositedFundsAccountId)
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

        if (TryCreateCrossPeriodInvoiceSlices(invoice, out _, out var secondPeriodInvoice))
            sourceIds.Add(GetInvoiceAccountingPeriodSourceId(invoice.InvoiceId, secondPeriodInvoice.AccountingPeriod));

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
                IncludeVoided = false,
                IncludeUnposted = true
            })).ToList();

            if (journalEntries.Count > 0)
                journalEntriesBySourceId[sourceId] = journalEntries;
        }

        return journalEntriesBySourceId;
    }

    private static (Guid? JournalEntryId, string JournalEntryCode) ResolvePaymentJournalEntryForDepositInvoice(SecurityDepositPaymentAllocation paymentAllocation, IReadOnlyDictionary<Guid, List<JournalEntry>> journalEntriesBySourceId, int undepositedFundsAccountId)
    {
        for (var index = paymentAllocation.SecurityDepositPaymentLedgerLineIds.Count - 1; index >= 0; index--)
        {
            var paymentLedgerLineId = paymentAllocation.SecurityDepositPaymentLedgerLineIds[index];
            if (!journalEntriesBySourceId.TryGetValue(paymentLedgerLineId, out var journalEntries) || journalEntries.Count == 0)
                continue;

            var paymentJournalEntry = SelectSecurityDepositPaymentJournalEntry(journalEntries, undepositedFundsAccountId);
            if (paymentJournalEntry == null)
                continue;

            return (paymentJournalEntry.JournalEntryId, paymentJournalEntry.JournalEntryCode?.Trim() ?? string.Empty);
        }

        return (null, string.Empty);
    }

    private static JournalEntry? SelectSecurityDepositPaymentJournalEntry(IReadOnlyList<JournalEntry> journalEntries, int undepositedFundsAccountId)
    {
        if (journalEntries.Count == 0)
            return null;

        var standardPayment = journalEntries.FirstOrDefault(entry =>
            entry.SourceTypeId == (int)SourceType.InvoicePayment
            && !entry.IsCashOnly
            && (entry.JournalEntryLines ?? []).Any(line => line.ChartOfAccountId == undepositedFundsAccountId && line.Debit > 0));
        if (standardPayment != null)
            return standardPayment;

        return journalEntries.FirstOrDefault(entry => entry.SourceTypeId == (int)SourceType.InvoicePayment && !entry.IsCashOnly);
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

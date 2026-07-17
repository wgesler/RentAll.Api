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

    public async Task<Reservation> ApplySecurityDepositReturnAsync(Guid reservationId, Guid organizationId, string officeAccess, int chartOfAccountId, string description, decimal amount, DateOnly paymentDate, PaymentType paymentType, Guid currentUser)
    {
        _ = paymentType;
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new Exception("Reservation not found");

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

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
        {
            reservation.DepositReturned = true;
            reservation.ModifiedBy = currentUser;
            return await _reservationRepository.UpdateByIdAsync(reservation);
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, reservation.OfficeId);
        var escrowSecurityDepositAccountId = GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, reservation.OfficeId, accountingOffice);
        var lineContext = await ResolveReservationJournalEntryLineContextAsync(organizationId, departure, reservation);
        var memo = string.IsNullOrWhiteSpace(description)
            ? $"{reservation.ReservationCode}: Security Deposit Return"
            : description.Trim();

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

        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = reservation.OfficeId,
            TransactionDate = paymentDate,
            SourceTypeId = (int)SourceType.SecurityDeposit,
            SourceId = reservation.ReservationId,
            SourceCode = reservation.ReservationCode,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { liabilityLine, bankLine },
            CreatedBy = currentUser
        };

        var priorPaid = await SumSecurityDepositReturnPaidAmountAsync(organizationId, reservation.OfficeId, reservation.ReservationId);
        var paymentAmount = RoundSecurityDepositAmount(Math.Abs(amount));

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);

        var totalPaid = RoundSecurityDepositAmount(priorPaid + paymentAmount);
        var depositOwed = RoundSecurityDepositAmount(Math.Abs(reservation.Deposit));
        reservation.DepositReturned = totalPaid >= depositOwed;
        reservation.ModifiedBy = currentUser;
        return await _reservationRepository.UpdateByIdAsync(reservation);
    }

    #endregion

    #region Report Enrichment

    private async Task EnrichSecurityDepositJournalEntryDataAsync(Guid organizationId, IList<ReservationDeparture> rows)
    {
        if (rows.Count == 0)
            return;

        var reservationIds = rows.Select(row => row.ReservationId).Distinct().ToList();
        var officeIds = rows.Select(row => row.OfficeId).Where(id => id > 0).Distinct().ToList();

        var invoiceEnrichment = await GetReservationInvoiceEnrichmentAsync(organizationId, reservationIds, officeIds);
        var returnedByReservation = await GetSecurityDepositReturnPaidAmountsByReservationAsync(organizationId, reservationIds, officeIds);

        foreach (var row in rows)
        {
            row.OwedAmount = invoiceEnrichment.OwedByReservation.TryGetValue(row.ReservationId, out var owedAmount)
                ? owedAmount
                : 0m;
            row.ReturnedAmount = returnedByReservation.TryGetValue(row.ReservationId, out var returnedAmount)
                ? returnedAmount
                : 0m;

            if (invoiceEnrichment.SecurityDepositInfoByReservation.TryGetValue(row.ReservationId, out var securityDepositInfo))
            {
                row.Deposit = securityDepositInfo.DepositAmount;
                row.PaidAmount = securityDepositInfo.PaidAmount;
                row.JournalEntryId = securityDepositInfo.DepositJournalEntryId;
                row.JournalEntryCode = securityDepositInfo.DepositJournalEntryCode;
                row.PaidJournalEntryId = securityDepositInfo.PaidJournalEntryId;
                row.PaidJournalEntryCode = securityDepositInfo.PaidJournalEntryCode;
                row.InvoiceId = securityDepositInfo.DepositInvoiceId;
                row.InvoiceCode = securityDepositInfo.DepositInvoiceCode;
                continue;
            }

            row.Deposit = 0m;
            row.PaidAmount = 0m;
            row.JournalEntryId = null;
            row.JournalEntryCode = string.Empty;
            row.PaidJournalEntryId = null;
            row.PaidJournalEntryCode = string.Empty;
            row.InvoiceId = null;
            row.InvoiceCode = string.Empty;
        }
    }

    private async Task<ReservationInvoiceEnrichment> GetReservationInvoiceEnrichmentAsync(Guid organizationId, IReadOnlyCollection<Guid> reservationIds, IReadOnlyCollection<int> officeIds)
    {
        var enrichment = new ReservationInvoiceEnrichment
        {
            OwedByReservation = reservationIds.ToDictionary(id => id, _ => 0m),
            SecurityDepositInfoByReservation = new Dictionary<Guid, SecurityDepositInvoiceInfo>()
        };

        if (reservationIds.Count == 0)
            return enrichment;

        var reservationIdSet = reservationIds.ToHashSet();
        var paymentJournalEntryByLedgerLineId = new Dictionary<Guid, (Guid JournalEntryId, string JournalEntryCode)>();

        foreach (var officeId in officeIds)
        {
            var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, officeId);
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            var securityDepositAccountIds = ResolveSecurityDepositChartOfAccountIds(chartOfAccounts, officeId, accountingOffice, costCodeById);

            var invoices = await GetInvoicesForReservationsAsync(organizationId, officeId, reservationIdSet);

            if (invoices.Count == 0)
                continue;

            var invoiceIdToReservationId = invoices.ToDictionary(
                invoice => invoice.InvoiceId,
                invoice => invoice.ReservationId!.Value);
            var invoiceIdToCode = invoices.ToDictionary(
                invoice => invoice.InvoiceId,
                invoice => invoice.InvoiceCode?.Trim() ?? string.Empty);
            var invoiceIdSet = invoiceIdToReservationId.Keys.ToHashSet();
            var depositInfoByReservation = reservationIdSet.ToDictionary(
                reservationId => reservationId,
                _ => new MutableSecurityDepositInvoiceInfo());

            foreach (var invoiceGroup in invoices.GroupBy(invoice => invoice.ReservationId!.Value))
            {
                var owedAmount = CalculateOutstandingInvoiceBalance(invoiceGroup);
                enrichment.OwedByReservation[invoiceGroup.Key] = RoundSecurityDepositAmount(
                    enrichment.OwedByReservation[invoiceGroup.Key] + owedAmount);
            }

            foreach (var securityDepositAccountId in securityDepositAccountIds)
            {
                var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
                {
                    OrganizationId = organizationId,
                    OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                    ChartOfAccountId = securityDepositAccountId,
                    SourceTypeId = (int)SourceType.Invoice,
                    IncludeVoided = false,
                    IncludeUnposted = true
                });

                foreach (var lineGroup in (lines ?? [])
                    .Where(line => line.SourceId.HasValue
                        && invoiceIdSet.Contains(line.SourceId.Value)
                        && line.Credit > 0m)
                    .GroupBy(line => invoiceIdToReservationId[line.SourceId!.Value]))
                {
                    var depositAmount = RoundSecurityDepositAmount(lineGroup.Sum(line => line.Credit));
                    var depositJournalEntryLine = lineGroup
                        .OrderBy(line => line.TransactionDate)
                        .ThenBy(line => line.CreatedOn)
                        .First();

                    var depositInfo = depositInfoByReservation[lineGroup.Key];
                    depositInfo.DepositAmount = RoundSecurityDepositAmount(depositInfo.DepositAmount + depositAmount);
                    depositInfo.DepositJournalEntryId ??= depositJournalEntryLine.JournalEntryId;
                    if (string.IsNullOrWhiteSpace(depositInfo.DepositJournalEntryCode))
                        depositInfo.DepositJournalEntryCode = depositJournalEntryLine.JournalEntryCode?.Trim() ?? string.Empty;

                    if (depositInfo.DepositInvoiceId == null && depositJournalEntryLine.SourceId is { } depositInvoiceId)
                    {
                        depositInfo.DepositInvoiceId = depositInvoiceId;
                        if (invoiceIdToCode.TryGetValue(depositInvoiceId, out var depositInvoiceCode))
                            depositInfo.DepositInvoiceCode = depositInvoiceCode;
                    }
                }
            }

            foreach (var invoice in invoices.OrderBy(item => item.InvoiceDate).ThenBy(item => item.InvoiceCode))
            {
                if (!invoice.ReservationId.HasValue)
                    continue;

                var paymentAllocation = CalculateSecurityDepositPaymentAllocation(invoice, costCodeById);
                if (paymentAllocation.PaidAmount == 0m)
                    continue;

                var depositInfo = depositInfoByReservation[invoice.ReservationId.Value];
                depositInfo.PaidAmount = RoundSecurityDepositAmount(depositInfo.PaidAmount + paymentAllocation.PaidAmount);

                if (paymentAllocation.LastPaymentLedgerLineId is not { } paymentLedgerLineId)
                    continue;

                depositInfo.LastPaymentLedgerLineId = paymentLedgerLineId;
                if (!paymentJournalEntryByLedgerLineId.ContainsKey(paymentLedgerLineId))
                    depositInfo.PaymentLedgerLineIds.Add(paymentLedgerLineId);
            }

            foreach (var depositInfo in depositInfoByReservation.Values)
            {
                foreach (var paymentLedgerLineId in depositInfo.PaymentLedgerLineIds)
                {
                    if (paymentJournalEntryByLedgerLineId.ContainsKey(paymentLedgerLineId))
                        continue;

                    var paymentJournalEntry = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
                    {
                        OrganizationId = organizationId,
                        OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                        SourceTypeId = (int)SourceType.InvoicePayment,
                        SourceId = paymentLedgerLineId,
                        IncludeVoided = false,
                        IncludeUnposted = true
                    })).FirstOrDefault();

                    if (paymentJournalEntry == null)
                        continue;

                    paymentJournalEntryByLedgerLineId[paymentLedgerLineId] = (
                        paymentJournalEntry.JournalEntryId,
                        paymentJournalEntry.JournalEntryCode?.Trim() ?? string.Empty);
                }
            }

            foreach (var (reservationId, depositInfo) in depositInfoByReservation)
            {
                if (depositInfo.DepositAmount == 0m && depositInfo.PaidAmount == 0m)
                    continue;

                Guid? paidJournalEntryId = null;
                var paidJournalEntryCode = string.Empty;
                if (depositInfo.LastPaymentLedgerLineId is { } lastPaymentLedgerLineId
                    && paymentJournalEntryByLedgerLineId.TryGetValue(lastPaymentLedgerLineId, out var paidJournalEntry))
                {
                    paidJournalEntryId = paidJournalEntry.JournalEntryId;
                    paidJournalEntryCode = paidJournalEntry.JournalEntryCode;
                }

                if (enrichment.SecurityDepositInfoByReservation.TryGetValue(reservationId, out var existing))
                {
                    enrichment.SecurityDepositInfoByReservation[reservationId] = new SecurityDepositInvoiceInfo
                    {
                        DepositAmount = RoundSecurityDepositAmount(existing.DepositAmount + depositInfo.DepositAmount),
                        PaidAmount = RoundSecurityDepositAmount(existing.PaidAmount + depositInfo.PaidAmount),
                        DepositJournalEntryId = existing.DepositJournalEntryId ?? depositInfo.DepositJournalEntryId,
                        DepositJournalEntryCode = !string.IsNullOrWhiteSpace(existing.DepositJournalEntryCode)
                            ? existing.DepositJournalEntryCode
                            : depositInfo.DepositJournalEntryCode,
                        PaidJournalEntryId = paidJournalEntryId ?? existing.PaidJournalEntryId,
                        PaidJournalEntryCode = !string.IsNullOrWhiteSpace(paidJournalEntryCode)
                            ? paidJournalEntryCode
                            : existing.PaidJournalEntryCode,
                        DepositInvoiceId = existing.DepositInvoiceId ?? depositInfo.DepositInvoiceId,
                        DepositInvoiceCode = !string.IsNullOrWhiteSpace(existing.DepositInvoiceCode)
                            ? existing.DepositInvoiceCode
                            : depositInfo.DepositInvoiceCode
                    };
                    continue;
                }

                enrichment.SecurityDepositInfoByReservation[reservationId] = new SecurityDepositInvoiceInfo
                {
                    DepositAmount = depositInfo.DepositAmount,
                    PaidAmount = depositInfo.PaidAmount,
                    DepositJournalEntryId = depositInfo.DepositJournalEntryId,
                    DepositJournalEntryCode = depositInfo.DepositJournalEntryCode,
                    PaidJournalEntryId = paidJournalEntryId,
                    PaidJournalEntryCode = paidJournalEntryCode,
                    DepositInvoiceId = depositInfo.DepositInvoiceId,
                    DepositInvoiceCode = depositInfo.DepositInvoiceCode
                };
            }
        }

        return enrichment;
    }

    private class ReservationInvoiceEnrichment
    {
        public Dictionary<Guid, decimal> OwedByReservation { get; init; } = new();
        public Dictionary<Guid, SecurityDepositInvoiceInfo> SecurityDepositInfoByReservation { get; init; } = new();
    }

    private class SecurityDepositInvoiceInfo
    {
        public decimal DepositAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public Guid? DepositJournalEntryId { get; init; }
        public string DepositJournalEntryCode { get; init; } = string.Empty;
        public Guid? PaidJournalEntryId { get; init; }
        public string PaidJournalEntryCode { get; init; } = string.Empty;
        public Guid? DepositInvoiceId { get; init; }
        public string DepositInvoiceCode { get; init; } = string.Empty;
    }

    private class MutableSecurityDepositInvoiceInfo
    {
        public decimal DepositAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public Guid? DepositJournalEntryId { get; set; }
        public string DepositJournalEntryCode { get; set; } = string.Empty;
        public Guid? DepositInvoiceId { get; set; }
        public string DepositInvoiceCode { get; set; } = string.Empty;
        public Guid? LastPaymentLedgerLineId { get; set; }
        public HashSet<Guid> PaymentLedgerLineIds { get; } = [];
    }

    #endregion

    #region Invoice Payment Allocation

    private class SecurityDepositPaymentAllocation
    {
        public decimal PaidAmount { get; init; }
        public Guid? LastPaymentLedgerLineId { get; init; }
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
        Guid? lastPaymentLedgerLineId = null;

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
                        lastPaymentLedgerLineId = payment.LedgerLineId;
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
                        lastPaymentLedgerLineId = payment.LedgerLineId;
                    }
                }
            }
        }

        return new SecurityDepositPaymentAllocation
        {
            PaidAmount = Math.Max(0m, securityDepositPaid),
            LastPaymentLedgerLineId = securityDepositPaid > 0m ? lastPaymentLedgerLineId : null
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

    private static decimal CalculateOutstandingInvoiceBalance(IEnumerable<Invoice> invoices)
        => (invoices ?? []).Sum(invoice => Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount));

    private HashSet<int> ResolveSecurityDepositChartOfAccountIds(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var accountIds = new HashSet<int>();

        try
        {
            accountIds.Add(GetDefaultEscrowSecurityDepositAccount(chartOfAccounts, officeId, accountingOffice));
        }
        catch
        {
            // Ignore offices missing escrow configuration.
        }

        var securityDepositCostCode = costCodeById.Values
            .FirstOrDefault(costCode => costCode.TransactionType == TransactionType.SecurityDeposit);
        if (securityDepositCostCode != null)
            accountIds.Add(GetDefaultTenantIncome(chartOfAccounts, officeId, accountingOffice, securityDepositCostCode));

        return accountIds;
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
            var account = chartOfAccounts.FirstOrDefault(item => item.AccountId == accountId);
            var accountLabel = FormatEscrowSecurityDepositAccountLabel(account);

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
            return (string.Empty, 0m);
        }
    }

    private static decimal SumEscrowLiabilityAccountBalance(IEnumerable<JournalEntryLineSearchResult> lines)
        => (lines ?? []).Sum(line => line.Credit - line.Debit);

    private static string FormatEscrowSecurityDepositAccountLabel(ChartOfAccount? account)
    {
        if (account == null)
            return string.Empty;

        return $"{account.AccountNo} {account.Name}".Trim();
    }

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
            .GroupBy(entry => entry.SourceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => RoundSecurityDepositAmount(group.Sum(CalculateSecurityDepositReturnJournalEntryAmount)));
    }

    private async Task<decimal> SumSecurityDepositReturnPaidAmountAsync(Guid organizationId, int officeId, Guid reservationId)
    {
        if (officeId <= 0 || reservationId == Guid.Empty)
            return 0m;

        var paidByReservation = await GetSecurityDepositReturnPaidAmountsByReservationAsync(organizationId, [reservationId], [officeId]);

        return paidByReservation.TryGetValue(reservationId, out var paidAmount)
            ? paidAmount
            : 0m;
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

    private static decimal RoundSecurityDepositAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static List<int> ParseOfficeIdsFromAccess(string officeAccess)
        => (officeAccess ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();

    #endregion
}

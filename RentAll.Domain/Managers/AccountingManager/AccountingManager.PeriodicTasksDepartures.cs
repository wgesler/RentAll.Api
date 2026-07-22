using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task<int> ProcessDepartureFeesAsync(Guid organizationId, string officeIds, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (rangeStart, rangeEnd) = ResolveDepartureFeeDateRange(startDate, endDate, today);
        if (rangeStart > rangeEnd)
            return 0;

        var departures = (await _reservationRepository.GetMonthlyDepartedReservationsAsync(organizationId, officeIds, rangeStart, rangeEnd)).ToList();

        if (logDecisions)
        {
            await LogDepartureFeeRunAsync(organizationId, ResolveFirstOfficeIdFromCsv(officeIds), rangeStart, rangeEnd, departures.Count, departures.Count == 0 ? "No reservations with departures in range" : "Processing reservations with departures in range");
        }

        await CreateJournalEntriesForDepartedReservationsAsync(organizationId, departures, cancellationToken, logDecisions);
        return departures.Count;
    }

    public async Task CreateJournalEntriesForDepartedReservationsAsync(Guid organizationId, IReadOnlyCollection<ReservationDeparture> reservations, CancellationToken cancellationToken, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;

        foreach (var reservation in reservations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await CreateJournalEntryForDepartedReservationAsync(organizationId, reservation, cancellationToken, logDecisions);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(trigger: "Departures", organizationId: organizationId, officeId: reservation.OfficeId, sourceTypeId: (int)SourceType.Reservation, sourceId: reservation.ReservationId, documentCode: reservation.ReservationCode, accountingPeriod: reservation.DepartureDate, amount: null, message: ex.Message, currentUser: SystemOrganization);
            }
        }
    }

    private async Task CreateJournalEntryForDepartedReservationAsync(Guid organizationId, ReservationDeparture reservation, CancellationToken cancellationToken, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // A reservation only departs once, so if a JE already exists, don't reprocess
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.Reservation,
            SourceId = reservation.ReservationId,
            IncludeVoided = true,
            IncludeUnposted = true
        });

        if (existingEntries.Any())
        {
            if (logDecisions)
            {
                await LogDepartureFeeDecisionAsync(organizationId, reservation.OfficeId, reservation.PropertyId, reservation.PropertyCode, reservation.ReservationCode, reservation.DepartureDate, amount: null, "Skipped — departure journal entry already exists.");
            }
            return;
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, reservation.OfficeId);
        var (office, costCodeById) = await LoadOfficeCostCodeContextAsync(organizationId, reservation.OfficeId);
        if (office == null)
            throw new Exception($"Office {reservation.OfficeId} was not found");

        costCodeById.TryGetValue(office.DepartureFeeCcId ?? 0, out var departureFeeCostCode);
        costCodeById.TryGetValue(office.PetFeeCcId ?? 0, out var petFeeCostCode);
        var reservationDetail = await _reservationRepository.GetReservationByIdAsync(reservation.ReservationId, organizationId);
        if (reservationDetail == null)
            throw new Exception($"Reservation {reservation.ReservationCode} was not found");

        var invoicedDepartureFeeTotal = await SumInvoicedLedgerLinesForReservationByCostCodeAsync(organizationId, reservation.OfficeId, reservation.ReservationId, office.DepartureFeeCcId ?? 0);
        var propertyDepartureFeeAmount = reservationDetail.DepartureFee > 0m ? reservationDetail.DepartureFee : 0m;
        var isDepartureFeeFromInvoice = invoicedDepartureFeeTotal > 0m;
        var departureFeeAmount = isDepartureFeeFromInvoice ? invoicedDepartureFeeTotal : propertyDepartureFeeAmount;
        var invoicedPetFeeTotal = await SumInvoicedLedgerLinesForReservationByCostCodeAsync(organizationId, reservation.OfficeId, reservation.ReservationId, office.PetFeeCcId ?? 0);
        var petFeeAmount = invoicedPetFeeTotal > 0m ? invoicedPetFeeTotal : (reservationDetail.PetFee > 0 ? reservationDetail.PetFee : 0m);
        if (departureFeeAmount == 0m && petFeeAmount == 0m)
        {
            if (logDecisions)
            {
                await LogDepartureFeeDecisionAsync(organizationId, reservation.OfficeId, reservation.PropertyId, reservation.PropertyCode, reservation.ReservationCode, reservation.DepartureDate, amount: null, $"Skipped — departure on {reservation.DepartureDate:MM/dd/yyyy} but departure fee and pet fee are zero.");
            }
            return;
        }

        var journalEntry = await BuildJournalEntryFromDepartedReservationAsync(organizationId, reservation, reservationDetail, chartOfAccounts, accountingOffice, departureFeeAmount, petFeeAmount, departureFeeCostCode, petFeeCostCode);

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);

        if (logDecisions)
        {
            var totalAmount = departureFeeAmount + petFeeAmount;
            var feeParts = new List<string>();
            if (departureFeeAmount > 0m)
                feeParts.Add($"departure fee ${departureFeeAmount:0.00}");
            if (petFeeAmount > 0m)
                feeParts.Add($"pet fee ${petFeeAmount:0.00}");

            await LogDepartureFeeDecisionAsync(organizationId, reservation.OfficeId, reservation.PropertyId, reservation.PropertyCode, reservation.ReservationCode, reservation.DepartureDate, amount: totalAmount, $"Created journal entry — {string.Join(", ", feeParts)}.");
        }
    }
    #endregion

    #region Journal Entries
    private async Task<JournalEntry> BuildJournalEntryFromDepartedReservationAsync(Guid organizationId, ReservationDeparture reservation, Reservation reservationDetail, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, decimal departureFeeAmount, decimal petFeeAmount, CostCode? departureFeeCostCode, CostCode? petFeeCostCode)
    {
        // AGENT-NOTE: DO NOT TOUCH.
        // DEPARTURES-JE-ACCOUNTS
        // Departure Fee:
        // Line 1 — Debit: Default departure account (GetDefaultDepartureAccount).
        // Line 2 — Credit: Departure income account (GetDefaultDepartureIncome).
        // Pet Fee:
        // Line 1 — Debit: Default pet account (GetDefaultPetAccount).
        // Line 2 — Credit: Departure income account (GetDefaultDepartureIncome).
        // END DEPARTURES-JE-ACCOUNTS

        var defaultDepartureAccountId = GetDefaultTenantExpense(chartOfAccounts, reservation.OfficeId, accountingOffice, departureFeeCostCode);
        var defaultPetAccountId = GetDefaultTenantExpense(chartOfAccounts, reservation.OfficeId, accountingOffice, petFeeCostCode);
        var defaultDepartureIncomeAccountId = GetDefaultDepartureIncome(chartOfAccounts, reservation.OfficeId, accountingOffice, departureFeeCostCode);
        var journalEntryLines = new List<JournalEntryLine>();
        var lineContext = await ResolveReservationJournalEntryLineContextAsync(organizationId, reservation, reservationDetail);

        if (departureFeeAmount > 0m)
        {
            var departureExpenseLine = new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureAccountId,
                CostCodeId = departureFeeCostCode?.CostCodeId,
                Debit = departureFeeAmount,
                Credit = 0m,
                Memo = BuildDepartureFeeMemo(reservation.ReservationCode),
                CreatedBy = SystemOrganization
            };
            ApplyJournalEntryLineContext(departureExpenseLine, lineContext);
            journalEntryLines.Add(departureExpenseLine);
            var departureIncomeLine = new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureIncomeAccountId,
                CostCodeId = departureFeeCostCode?.CostCodeId,
                Debit = 0m,
                Credit = departureFeeAmount,
                Memo = BuildDepartureFeeIncomeMemo(reservation.ReservationCode),
                CreatedBy = SystemOrganization
            };
            ApplyJournalEntryLineContext(departureIncomeLine, lineContext);
            journalEntryLines.Add(departureIncomeLine);
        }

        if (petFeeAmount > 0m)
        {
            var petExpenseLine = new JournalEntryLine
            {
                ChartOfAccountId = defaultPetAccountId,
                CostCodeId = petFeeCostCode?.CostCodeId,
                Debit = petFeeAmount,
                Credit = 0m,
                Memo = BuildPetFeeMemo(reservation.ReservationCode),
                CreatedBy = SystemOrganization
            };
            ApplyJournalEntryLineContext(petExpenseLine, lineContext);
            journalEntryLines.Add(petExpenseLine);
            var petIncomeLine = new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureIncomeAccountId,
                CostCodeId = petFeeCostCode?.CostCodeId,
                Debit = 0m,
                Credit = petFeeAmount,
                Memo = BuildDepartureFeeIncomeMemo(reservation.ReservationCode),
                CreatedBy = SystemOrganization
            };
            ApplyJournalEntryLineContext(petIncomeLine, lineContext);
            journalEntryLines.Add(petIncomeLine);
        }

        return ClassifyJournalEntry(new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = reservation.OfficeId,
            TransactionDate = reservation.DepartureDate,
            AccountingPeriod = new DateOnly(reservation.DepartureDate.Year, reservation.DepartureDate.Month, 1),
            SourceTypeId = (int)SourceType.Reservation,
            SourceId = reservation.ReservationId,
            SourceCode = ResolveJournalEntrySourceCodeFromReservation(reservation),
            Memo = BuildDeparturesMemo(reservation.ReservationCode),
            JournalEntryLines = journalEntryLines,
            CreatedBy = SystemOrganization
        }, JournalEntryKind.DepartureFee, Perspective.Company);
    }
    #endregion

    #region Helpers
    private async Task<decimal> SumInvoicedLedgerLinesForReservationByCostCodeAsync(Guid organizationId, int officeId, Guid reservationId, int costCodeId)
    {
        if (costCodeId <= 0)
            return 0m;

        var invoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            ReservationId = reservationId,
            IncludeInactive = true,
            IncludePaid = true
        });

        return invoices
            .SelectMany(i => i.LedgerLines)
            .Where(line => line.Amount != 0m)
            .Where(line => line.CostCodeId == costCodeId)
            .Sum(line => line.Amount);
    }

    private async Task LogDepartureFeeRunAsync(Guid organizationId, int? officeId, DateOnly rangeStart, DateOnly rangeEnd, int reservationCount, string message)
    {
        var fullMessage = reservationCount > 0
            ? $"Departure fee sync ({rangeStart:MM/dd/yyyy}-{rangeEnd:MM/dd/yyyy}): {message} ({reservationCount} reservation(s) with departures in range)."
            : $"Departure fee sync ({rangeStart:MM/dd/yyyy}-{rangeEnd:MM/dd/yyyy}): {message}.";

        await LogAccountingLogAsync(new AccountingLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = null,
            Message = fullMessage
        });
    }

    private async Task LogDepartureFeeDecisionAsync(Guid organizationId, int officeId, Guid propertyId, string propertyCode, string reservationCode, DateOnly departureDate, decimal? amount, string message)
    {
        var fullMessage = $"Departure fee [{propertyCode}] {reservationCode} (departs {departureDate:MM/dd/yyyy}): {message}";
        await LogPeriodicAccountingDecisionAsync(organizationId, officeId, propertyId, amount, fullMessage);
    }

    private static (DateOnly RangeStart, DateOnly RangeEnd) ResolveDepartureFeeDateRange(DateOnly? startDate, DateOnly? endDate, DateOnly today)
    {
        if (!startDate.HasValue && !endDate.HasValue)
            return (new DateOnly(today.Year, today.Month, 1), today);

        var rangeStart = startDate ?? endDate!.Value;
        var rangeEnd = endDate ?? startDate!.Value;
        if (rangeStart > rangeEnd)
            (rangeStart, rangeEnd) = (rangeEnd, rangeStart);

        if (rangeEnd > today)
            rangeEnd = today;

        return (rangeStart, rangeEnd);
    }

    private static int? ResolveFirstOfficeIdFromCsv(string officeIds)
    {
        foreach (var segment in officeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(segment, out var officeId) && officeId > 0)
                return officeId;
        }

        return null;
    }

    private async Task LogPeriodicAccountingDecisionAsync(Guid organizationId, int officeId, Guid propertyId, decimal? amount, string message)
    {
        await LogAccountingLogAsync(new AccountingLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = propertyId,
            OriginalAmount = amount,
            Message = message
        });
    }
    #endregion
}

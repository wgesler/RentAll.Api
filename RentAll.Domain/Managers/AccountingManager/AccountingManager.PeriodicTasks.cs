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

        await CreateJournalEntriesForDepartedReservationAsync(organizationId, departures, cancellationToken, logDecisions);
        return departures.Count;
    }

    public async Task CreateJournalEntriesForDepartedReservationAsync(Guid organizationId, IReadOnlyCollection<ReservationList> reservations, CancellationToken cancellationToken, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;

        foreach (var reservation in reservations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await CreateJournalEntriesForDepartedReservationAsync(organizationId, reservation, cancellationToken, logDecisions);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(trigger: "Departures", organizationId: organizationId, officeId: reservation.OfficeId, sourceTypeId: (int)SourceType.Reservation, sourceId: reservation.ReservationId, documentCode: reservation.ReservationCode, accountingPeriod: reservation.DepartureDate, amount: null, message: ex.Message, currentUser: SystemOrganization);
            }
        }
    }

    public async Task CreateJournalEntriesForLinensAndTowelsAsync(Guid organizationId, IReadOnlyCollection<PropertyAgreement> monthlyAgreements, IReadOnlyCollection<PropertyAgreement> annualAgreements, CancellationToken cancellationToken, DateOnly? processingDate = null)
    {
        // Periodic scheduler passes today's date (occupancy checked from the 1st through that date).
        // Sync passes the last day of each month in the selected criteria range.
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;
        var runDate = processingDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var processAnnualAgreements = runDate.Month == 1 && runDate.Day == 1;

        foreach (var monthlyAgreement in monthlyAgreements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await CreateJournalEntriesForLinensAndTowelsAsync(organizationId, monthlyAgreement, isMonthly: true, runDate, cancellationToken);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(trigger: "LinensAndTowels", organizationId: organizationId, officeId: monthlyAgreement.OfficeId, sourceTypeId: (int)SourceType.LinensAndTowels, sourceId: monthlyAgreement.PropertyId, documentCode: $"Property-{monthlyAgreement.PropertyId}", accountingPeriod: null, amount: monthlyAgreement.LinenAndTowelFee, message: ex.Message, currentUser: SystemOrganization);
            }
        }

        if (processAnnualAgreements)
        {
            foreach (var annualAgreement in annualAgreements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await CreateJournalEntriesForLinensAndTowelsAsync(organizationId, annualAgreement, isMonthly: false, runDate, cancellationToken);
                }
                catch (Exception ex)
                {
                    await LogAccountingErrorAsync(trigger: "LinensAndTowels", organizationId: organizationId, officeId: annualAgreement.OfficeId, sourceTypeId: (int)SourceType.LinensAndTowels, sourceId: annualAgreement.PropertyId, documentCode: $"Property-{annualAgreement.PropertyId}", accountingPeriod: null, amount: annualAgreement.LinenAndTowelFee, message: ex.Message, currentUser: SystemOrganization);
                }
            }
        }
    }
    #endregion

    #region Journal Entry
    private async Task CreateJournalEntriesForDepartedReservationAsync(Guid organizationId, ReservationList reservation, CancellationToken cancellationToken, bool logDecisions = false)
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
        var defaultDepartureAccountId = GetDefaultTenantExpense(chartOfAccounts, reservation.OfficeId, accountingOffice, departureFeeCostCode);
        var defaultPetAccountId = GetDefaultTenantExpense(chartOfAccounts, reservation.OfficeId, accountingOffice, petFeeCostCode);
        var defaultDepartureIncomeAccountId = GetDefaultDepartureIncome(chartOfAccounts, reservation.OfficeId, accountingOffice, departureFeeCostCode);
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

        var journalEntryLines = new List<JournalEntryLine>();
        var lineContext = await ResolveReservationJournalEntryLineContextAsync(organizationId, reservation, reservationDetail);
        if (departureFeeAmount > 0m)
        {
            var departureExpenseLine = new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureAccountId,
                CostCodeId = departureFeeCostCode,
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
                CostCodeId = departureFeeCostCode,
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
                CostCodeId = petFeeCostCode,
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
                CostCodeId = petFeeCostCode,
                Debit = 0m,
                Credit = petFeeAmount,
                Memo = BuildDepartureFeeIncomeMemo(reservation.ReservationCode),
                CreatedBy = SystemOrganization
            };
            ApplyJournalEntryLineContext(petIncomeLine, lineContext);
            journalEntryLines.Add(petIncomeLine);
        }

        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = reservation.OfficeId,
            TransactionDate = reservation.DepartureDate,
            PostingDate = new DateOnly(reservation.DepartureDate.Year, reservation.DepartureDate.Month, 1),
            SourceTypeId = (int)SourceType.Reservation,
            SourceId = reservation.ReservationId,
            SourceCode = ResolveJournalEntrySourceCodeFromReservation(reservation),
            Memo = BuildDeparturesMemo(reservation.ReservationCode),
            JournalEntryLines = journalEntryLines,
            CreatedBy = SystemOrganization
        };

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

    private async Task CreateJournalEntriesForLinensAndTowelsAsync(Guid organizationId, PropertyAgreement agreement, bool isMonthly, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var property = await _propertyRepository.GetPropertyByIdAsync(agreement.PropertyId, organizationId)
            ?? throw new Exception($"Property {agreement.PropertyId} was not found");
        var propertyCode = property.PropertyCode;
        if (property.PropertyLeaseType != PropertyLeaseType.PropertyManagement)
        {
            await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly, processingDate, amount: null, "Skipped — property is not Property Management lease type.");
            return;
        }

        var availableFrom = property.AvailableFrom;
        var availableUntil = property.AvailableUntil;

        if (isMonthly)
            await CreateMonthlyJournalEntriesForLinensAndTowelsAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, processingDate, cancellationToken);
        else
            await CreateAnnualJournalEntriesForLinensAndTowelsAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, processingDate, cancellationToken);
    }

    private static DateOnly ResolveMonthStart(DateOnly processingDate)
        => new(processingDate.Year, processingDate.Month, 1);

    private static DateOnly ResolveMonthEnd(DateOnly processingDate)
        => ResolveMonthStart(processingDate).AddMonths(1).AddDays(-1);

    private async Task<bool> HasLinensAndTowelsJournalEntryAsync(Guid organizationId, PropertyAgreement agreement, DateOnly startDate, DateOnly endDate)
    {
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = agreement.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.LinensAndTowels,
            SourceId = agreement.PropertyId,
            IncludeVoided = true,
            IncludeUnposted = true,
            StartDate = startDate,
            EndDate = endDate
        });

        return existingEntries.Any();
    }

    private async Task CreateMonthlyJournalEntriesForLinensAndTowelsAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var monthStart = ResolveMonthStart(processingDate);
        var monthEnd = ResolveMonthEnd(processingDate);
        if (await HasLinensAndTowelsJournalEntryAsync(organizationId, agreement, monthStart, monthEnd))
        {
            await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly: true, processingDate, amount: null, "Skipped — journal entry already exists for this month.");
            return;
        }

        var wasRentedThisMonth = await _reservationRepository.WasRentedThisMonthAsync(agreement.PropertyId, organizationId, processingDate);

        if (!wasRentedThisMonth)
        {
            await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly: true, processingDate, amount: null, $"Skipped — no non-owner rental from {monthStart:MM/dd/yyyy} through {processingDate:MM/dd/yyyy}.");
            return;
        }

        await CreateLinenAndTowelEntriesAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, isMonthly: true, processingDate, cancellationToken);
    }

    private async Task CreateAnnualJournalEntriesForLinensAndTowelsAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (processingDate.Month != 1 || processingDate.Day != 1)
        {
            await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly: false, processingDate, amount: null, "Skipped — annual linen and towel only runs on January 1.");
            return;
        }

        if (await HasLinensAndTowelsJournalEntryAsync(organizationId, agreement, processingDate, processingDate))
        {
            await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly: false, processingDate, amount: null, "Skipped — journal entry already exists for this date.");
            return;
        }

        await CreateLinenAndTowelEntriesAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, isMonthly: false, processingDate, cancellationToken);
    }

    private async Task CreateLinenAndTowelEntriesAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, bool isMonthly, DateOnly processingDate, CancellationToken cancellationToken)
    {
        // AGENT-NOTE: DO NOT TOUCH.
        // LINENS-AND-TOWELS-JE-ACCOUNTS
        // Monthly and annual linens/towels journal entries are created from PropertyAgreement data.
        // Annual proration rule:
        // - Initial year bills full PropertyAgreement.LinenAndTowelFee.
        // - In the second year after AvailableFrom, annual fee is prorated by 365 and billed
        //   only for the days the property was online (bounded by AvailableFrom/AvailableUntil).
        // Annual offboarding rule:
        // - In the AvailableUntil month/year, post the unused annual portion from that year.
        // - Unused annual portion uses a 365-day basis: annualFee/365 * (365 - onlineDaysFromJan1ToAvailableUntil).
        // - For this offboarding adjustment only, line directions are reversed (Debit Linen/Towel Income, Credit Owner A/P).
        // Line 1 — Debit: Owner Accounts Payable (GetDefaultOwnerAccountsPayable) for PropertyAgreement.LinenAndTowelFee.
        // Line 2 — Credit: Linen and Towel Income (GetDefaultLinenAndTowelIncome) for PropertyAgreement.LinenAndTowelFee.
        // If the fee is zero or negative, no journal entry is created.
        // END LINENS-AND-TOWELS-JE-ACCOUNTS

        cancellationToken.ThrowIfCancellationRequested();
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, agreement.OfficeId);
        var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, agreement.OfficeId, accountingOffice);
        var linenAndTowelIncomeAccountId = GetDefaultLinenAndTowelIncome(chartOfAccounts, agreement.OfficeId, accountingOffice);
        var isOffboardProrationMonth = !isMonthly &&
            availableUntil.HasValue &&
            availableUntil.Value.Month == processingDate.Month &&
            availableUntil.Value.Year == processingDate.Year;
        var feeAmount = isMonthly ? agreement.LinenAndTowelFee
            : isOffboardProrationMonth
                ? ResolveOffboardProrationAmount(agreement.LinenAndTowelFee, availableUntil!.Value)
                : ResolveOnboardProrationAmount(agreement.LinenAndTowelFee, availableFrom, availableUntil, processingDate);
        var reverseEntryDirection = isOffboardProrationMonth && feeAmount > 0m;
        if (feeAmount <= 0m)
        {
            var skipReason = isMonthly
                ? agreement.LinenAndTowelFee <= 0m
                    ? "Skipped — linen and towel fee is zero or negative."
                    : "Skipped — calculated fee amount is zero or negative."
                : ResolveAnnualLinenSkipReason(agreement.LinenAndTowelFee, availableFrom, availableUntil, processingDate, isOffboardProrationMonth);
            await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly, processingDate, amount: null, skipReason);
            return;
        }
        var cadenceLabel = isMonthly ? "Monthly" : "Annual";

        _ = availableFrom;
        _ = availableUntil;
        _ = cadenceLabel;
        var ownerApDebit = reverseEntryDirection ? 0m : feeAmount;
        var ownerApCredit = reverseEntryDirection ? feeAmount : 0m;
        var incomeDebit = reverseEntryDirection ? feeAmount : 0m;
        var incomeCredit = reverseEntryDirection ? 0m : feeAmount;

        var journalEntryMemo = reverseEntryDirection
            ? BuildLinenAndTowelUnusedMemo(propertyCode)
            : BuildLinenAndTowelJournalMemo(propertyCode, isMonthly);
        var ownerLineMemo = reverseEntryDirection
            ? BuildOwnerLinenAndTowelUnusedMemo(propertyCode)
            : BuildOwnerLinenAndTowelMemo(propertyCode, isMonthly);
        var incomeLineMemo = reverseEntryDirection
            ? BuildLinenAndTowelUnusedMemo(propertyCode)
            : BuildLinenAndTowelIncomeMemo(propertyCode, isMonthly);

        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = agreement.OfficeId,
            TransactionDate = processingDate,
            SourceTypeId = (int)SourceType.LinensAndTowels,
            SourceId = agreement.PropertyId,
            Memo = journalEntryMemo,
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    ChartOfAccountId = ownerAccountsPayableAccountId,
                    PropertyId = agreement.PropertyId,
                    Debit = ownerApDebit,
                    Credit = ownerApCredit,
                    Memo = ownerLineMemo,
                    CreatedBy = SystemOrganization
                },
                new JournalEntryLine
                {
                    ChartOfAccountId = linenAndTowelIncomeAccountId,
                    PropertyId = agreement.PropertyId,
                    Debit = incomeDebit,
                    Credit = incomeCredit,
                    Memo = incomeLineMemo,
                    CreatedBy = SystemOrganization
                }
            ],
            CreatedBy = SystemOrganization
        };

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
        await LogLinenAndTowelDecisionAsync(organizationId, agreement.OfficeId, agreement.PropertyId, propertyCode, isMonthly, processingDate, amount: feeAmount, reverseEntryDirection ? "Created unused-portion journal entry." : "Created journal entry.");
    }

    private static string ResolveAnnualLinenSkipReason(decimal annualFeeAmount, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, bool isOffboardProrationMonth)
    {
        if (annualFeeAmount <= 0m)
            return "Skipped — linen and towel fee is zero or negative.";

        if (isOffboardProrationMonth)
            return "Skipped — no unused annual portion to bill at offboarding.";

        if (availableFrom == null)
            return "Skipped — calculated fee amount is zero or negative.";

        if (processingDate < availableFrom.Value &&
            !(availableFrom.Value.Month == processingDate.Month && availableFrom.Value.Year == processingDate.Year))
            return $"Skipped — property is not online until {availableFrom.Value:MM/dd/yyyy}.";

        return "Skipped — calculated fee amount is zero after annual proration.";
    }

    private static decimal ResolveOnboardProrationAmount(decimal annualFeeAmount, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate)
    {
        if (annualFeeAmount <= 0m || availableFrom == null)
            return annualFeeAmount;

        var isAvailableFromCurrentMonthAndYear =
            availableFrom.Value.Month == processingDate.Month &&
            availableFrom.Value.Year == processingDate.Year;

        // Do not bill before the property is online, except when we are
        // processing the same month/year as AvailableFrom (initial annual bill).
        if (processingDate < availableFrom.Value && !isAvailableFromCurrentMonthAndYear)
            return 0m;

        // Proration is allowed ONLY during the second year after AvailableFrom.
        // Example: AvailableFrom in 2022 => proration can happen in 2023 only.
        var isSecondYearProration = processingDate.Year == availableFrom.Value.Year + 1;
        if (!isSecondYearProration)
            return annualFeeAmount;

        var priorYearEnd = new DateOnly(processingDate.Year - 1, 12, 31);
        var onlineStart = availableFrom.Value;
        var onlineEnd = availableUntil.HasValue && availableUntil.Value < priorYearEnd
            ? availableUntil.Value
            : priorYearEnd;

        if (onlineEnd < onlineStart)
            return 0m;

        var onlineDays = onlineEnd.DayNumber - onlineStart.DayNumber + 1;
        return Math.Round(annualFeeAmount / 365m * onlineDays, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveOffboardProrationAmount(decimal annualFeeAmount, DateOnly availableUntil)
    {
        if (annualFeeAmount <= 0m)
            return annualFeeAmount;

        var startOfYear = new DateOnly(availableUntil.Year, 1, 1);
        var onlineDays = availableUntil.DayNumber - startOfYear.DayNumber + 1;
        if (onlineDays <= 0)
            return 0m;

        var offlineDays = 365 - onlineDays;
        if (offlineDays <= 0)
            return 0m;

        return Math.Round(annualFeeAmount / 365m * offlineDays, 2, MidpointRounding.AwayFromZero);
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

    private async Task LogLinenAndTowelDecisionAsync(Guid organizationId, int officeId, Guid propertyId, string propertyCode, bool isMonthly, DateOnly processingDate, decimal? amount, string message)
    {
        var cadence = isMonthly ? "Monthly" : "Annual";
        var fullMessage = $"{cadence} linen & towel [{propertyCode}] (as of {processingDate:MM/dd/yyyy}): {message}";
        await LogPeriodicAccountingDecisionAsync(organizationId, officeId, propertyId, amount, fullMessage);
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
    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task CreateJournalEntiesForDepartedReservationAsync(Guid organizationId, IReadOnlyCollection<ReservationList> reservations, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;

        foreach (var reservation in reservations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await BuildJournalEntiesForDepartedReservationAsync(organizationId, reservation, cancellationToken);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(
                    trigger: "Departures",
                    organizationId: organizationId,
                    officeId: reservation.OfficeId,
                    sourceTypeId: (int)SourceType.Reservation,
                    sourceId: reservation.ReservationId,
                    documentCode: reservation.ReservationCode,
                    accountingPeriod: reservation.DepartureDate,
                    amount: null,
                    message: ex.Message,
                    currentUser: SystemOrganization);
            }
        }
    }

    public async Task CreateJournalEntriesForLinensAndTowelsAsync(Guid organizationId, IReadOnlyCollection<PropertyAgreement> monthlyAgreements, IReadOnlyCollection<PropertyAgreement> annualAgreements, CancellationToken cancellationToken, DateOnly? processingDate = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;
        var runDate = processingDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var monthlyAgreement in monthlyAgreements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await BuildJournalEntriesForLinensAndTowelsAsync(organizationId, monthlyAgreement, isMonthly: true, runDate, cancellationToken);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(
                    trigger: "LinensAndTowels",
                    organizationId: organizationId,
                    officeId: monthlyAgreement.OfficeId,
                    sourceTypeId: (int)SourceType.LinensAndTowels,
                    sourceId: monthlyAgreement.PropertyId,
                    documentCode: $"Property-{monthlyAgreement.PropertyId}",
                    accountingPeriod: null,
                    amount: monthlyAgreement.LinenAndTowelFee,
                    message: ex.Message,
                    currentUser: SystemOrganization);
            }
        }

        foreach (var annualAgreement in annualAgreements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await BuildJournalEntriesForLinensAndTowelsAsync(organizationId, annualAgreement, isMonthly: false, runDate, cancellationToken);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(
                    trigger: "LinensAndTowels",
                    organizationId: organizationId,
                    officeId: annualAgreement.OfficeId,
                    sourceTypeId: (int)SourceType.LinensAndTowels,
                    sourceId: annualAgreement.PropertyId,
                    documentCode: $"Property-{annualAgreement.PropertyId}",
                    accountingPeriod: null,
                    amount: annualAgreement.LinenAndTowelFee,
                    message: ex.Message,
                    currentUser: SystemOrganization);
            }
        }
    }
    #endregion

    #region Journal Entry
    private async Task BuildJournalEntiesForDepartedReservationAsync(Guid organizationId, ReservationList reservation, CancellationToken cancellationToken)
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

        // A reservation only departs once, so if JE already exist, don't reprocess
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
            return;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, reservation.OfficeId);
        var (office, costCodeById) = await LoadOfficeCostCodeContextAsync(organizationId, reservation.OfficeId);
        if (office == null)
            throw new Exception($"Office {reservation.OfficeId} was not found");

        var defaultDepartureAccountId = GetDefaultDepartureAccount(chartOfAccounts, reservation.OfficeId, office, costCodeById, accountingOffice);
        var defaultPetAccountId = GetDefaultPetAccount(chartOfAccounts, reservation.OfficeId, office, costCodeById, accountingOffice);
        var defaultDepartureIncomeAccountId = GetDefaultDepartureIncome(chartOfAccounts, reservation.OfficeId, accountingOffice);
        var reservationDetail = await _reservationRepository.GetReservationByIdAsync(reservation.ReservationId, organizationId);
        if (reservationDetail == null)
            throw new Exception($"Reservation {reservation.ReservationCode} was not found");

        var invoicedDepartureFeeTotal = await SumDepartureFeeLinesForReservationAsync(organizationId, reservation.OfficeId, reservation.ReservationId);
        var propertyDepartureFeeAmount = reservationDetail.DepartureFee > 0m ? reservationDetail.DepartureFee : 0m;
        var isDepartureFeeFromInvoice = invoicedDepartureFeeTotal > 0m;
        var departureFeeAmount = isDepartureFeeFromInvoice ? invoicedDepartureFeeTotal : propertyDepartureFeeAmount;
        var petFeeAmount = reservationDetail.PetFee > 0 ? reservationDetail.PetFee : 0m;
        if (departureFeeAmount == 0m && petFeeAmount == 0m)
            return;

        var journalEntryLines = new List<JournalEntryLine>();
        if (departureFeeAmount > 0m)
        {
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureAccountId,
                ReservationId = reservation.ReservationId,
                PropertyId = reservation.PropertyId,
                Debit = departureFeeAmount,
                Credit = 0m,
                Memo = $"Departure Fee - {reservation.ReservationCode}",
                CreatedBy = SystemOrganization
            });
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureIncomeAccountId,
                ReservationId = reservation.ReservationId,
                PropertyId = reservation.PropertyId,
                Debit = 0m,
                Credit = departureFeeAmount,
                Memo = $"Departure Fee Income - {reservation.ReservationCode}",
                CreatedBy = SystemOrganization
            });
        }

        if (petFeeAmount > 0m)
        {
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = defaultPetAccountId,
                ReservationId = reservation.ReservationId,
                PropertyId = reservation.PropertyId,
                Debit = petFeeAmount,
                Credit = 0m,
                Memo = $"Pet Fee - {reservation.ReservationCode}",
                CreatedBy = SystemOrganization
            });
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = defaultDepartureIncomeAccountId,
                ReservationId = reservation.ReservationId,
                PropertyId = reservation.PropertyId,
                Debit = 0m,
                Credit = petFeeAmount,
                Memo = $"Departure Fee Income - {reservation.ReservationCode}",
                CreatedBy = SystemOrganization
            });
        }

        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = reservation.OfficeId,
            TransactionDate = reservation.DepartureDate,
            PostingDate = reservation.DepartureDate,
            SourceTypeId = (int)SourceType.Reservation,
            SourceId = reservation.ReservationId,
            Memo = $"Departures - {reservation.ReservationCode}",
            JournalEntryLines = journalEntryLines,
            CreatedBy = SystemOrganization
        };

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
    }

    private async Task<decimal> SumDepartureFeeLinesForReservationAsync(Guid organizationId, int officeId, Guid reservationId)
    {
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
            .Where(line => string.Equals(line.Description?.Trim(), "Departure Fee", StringComparison.OrdinalIgnoreCase))
            .Sum(line => line.Amount);
    }

    private async Task BuildJournalEntriesForLinensAndTowelsAsync(Guid organizationId, PropertyAgreement agreement, bool isMonthly, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = agreement.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.LinensAndTowels,
            SourceId = agreement.PropertyId,
            IncludeVoided = true,
            IncludeUnposted = true,
            StartDate = processingDate,
            EndDate = processingDate
        });

        if (existingEntries.Any())
            return;

        var property = await _propertyRepository.GetPropertyByIdAsync(agreement.PropertyId, organizationId)
            ?? throw new Exception($"Property {agreement.PropertyId} was not found");
        if (property.PropertyLeaseType != PropertyLeaseType.PropertyManagement)
            return;

        var availableFrom = property.AvailableFrom;
        var availableUntil = property.AvailableUntil;
        var propertyCode = property.PropertyCode;


        if (isMonthly)
            await BuildMonthlyJournalEntriesForLinensAndTowelsAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, processingDate, cancellationToken);
        else
            await BuildAnnualJournalEntriesForLinensAndTowelsAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, processingDate, cancellationToken);
    }

    private async Task BuildMonthlyJournalEntriesForLinensAndTowelsAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var wasRentedPreviousMonth = await _reservationRepository.WasRentedPreviousMonthAsync(agreement.PropertyId, organizationId, processingDate);
        if (!wasRentedPreviousMonth)
            wasRentedPreviousMonth = await WasRentedPreviousMonthFallbackAsync(agreement.PropertyId, organizationId, processingDate, cancellationToken);

        if (!wasRentedPreviousMonth)
            return;

        await BuildLinenAndTowelEntriesAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, isMonthly: true, processingDate, cancellationToken);
    }

    private async Task BuildAnnualJournalEntriesForLinensAndTowelsAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var isAvailableFromCurrentMonthAndYear = availableFrom.HasValue &&
            availableFrom.Value.Month == processingDate.Month &&
            availableFrom.Value.Year == processingDate.Year;
        var isAvailableUntilCurrentMonthAndYear = availableUntil.HasValue &&
            availableUntil.Value.Month == processingDate.Month &&
            availableUntil.Value.Year == processingDate.Year;

        if (!isAvailableFromCurrentMonthAndYear && !isAvailableUntilCurrentMonthAndYear && processingDate.Month != 1)
            return;

        await BuildLinenAndTowelEntriesAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, isMonthly: false, processingDate, cancellationToken);
    }

    private async Task BuildLinenAndTowelEntriesAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, bool isMonthly, DateOnly processingDate, CancellationToken cancellationToken)
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
            return;
        var cadenceLabel = isMonthly ? "Monthly" : "Annual";

        _ = availableFrom;
        _ = availableUntil;
        var ownerApDebit = reverseEntryDirection ? 0m : feeAmount;
        var ownerApCredit = reverseEntryDirection ? feeAmount : 0m;
        var incomeDebit = reverseEntryDirection ? feeAmount : 0m;
        var incomeCredit = reverseEntryDirection ? 0m : feeAmount;

        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = agreement.OfficeId,
            TransactionDate = processingDate,
            PostingDate = processingDate,
            SourceTypeId = (int)SourceType.LinensAndTowels,
            SourceId = agreement.PropertyId,
            Memo = reverseEntryDirection
                ? $"{cadenceLabel} Linen & Towel Unused Portion"
                : $"{cadenceLabel} Linen & Towel",
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    ChartOfAccountId = ownerAccountsPayableAccountId,
                    PropertyId = agreement.PropertyId,
                    Debit = ownerApDebit,
                    Credit = ownerApCredit,
                    Memo = reverseEntryDirection
                        ? $"Owner: {cadenceLabel}: Linen & Towel Unused Portion"
                        : $"Owner: {cadenceLabel}: Linen & Towel",
                    CreatedBy = SystemOrganization
                },
                new JournalEntryLine
                {
                    ChartOfAccountId = linenAndTowelIncomeAccountId,
                    PropertyId = agreement.PropertyId,
                    Debit = incomeDebit,
                    Credit = incomeCredit,
                    Memo = reverseEntryDirection
                        ? $"{cadenceLabel} Linen & Towel Unused Portion - Income"
                        : $"{cadenceLabel} Linen & Towel Income",
                    CreatedBy = SystemOrganization
                }
            ],
            CreatedBy = SystemOrganization
        };

        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
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

    private async Task<bool> WasRentedPreviousMonthFallbackAsync(Guid propertyId, Guid organizationId, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reservations = await _reservationRepository.GetReservationListByPropertyIdAsync(propertyId, organizationId);
        if (!reservations.Any())
            return false;

        var startOfCurrentMonth = new DateOnly(processingDate.Year, processingDate.Month, 1);
        var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);
        return reservations.Any(r =>
            r.ReservationType != ReservationType.Owner &&
            r.ArrivalDate < startOfCurrentMonth &&
            r.DepartureDate >= startOfPreviousMonth);
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

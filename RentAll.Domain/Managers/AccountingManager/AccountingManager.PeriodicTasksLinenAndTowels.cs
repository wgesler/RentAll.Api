using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
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
                await CreateJournalEntryForLinenAndTowelAsync(organizationId, monthlyAgreement, isMonthly: true, runDate, cancellationToken);
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
                    await CreateJournalEntryForLinenAndTowelAsync(organizationId, annualAgreement, isMonthly: false, runDate, cancellationToken);
                }
                catch (Exception ex)
                {
                    await LogAccountingErrorAsync(trigger: "LinensAndTowels", organizationId: organizationId, officeId: annualAgreement.OfficeId, sourceTypeId: (int)SourceType.LinensAndTowels, sourceId: annualAgreement.PropertyId, documentCode: $"Property-{annualAgreement.PropertyId}", accountingPeriod: null, amount: annualAgreement.LinenAndTowelFee, message: ex.Message, currentUser: SystemOrganization);
                }
            }
        }
    }

    private async Task CreateJournalEntryForLinenAndTowelAsync(Guid organizationId, PropertyAgreement agreement, bool isMonthly, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var property = await _propertyRepository.GetPropertyByIdAsync(agreement.PropertyId, organizationId)
            ?? throw new Exception($"Property {agreement.PropertyId} was not found");
        var propertyCode = property.PropertyCode;
        if (property.PropertyLeaseType != PropertyLeaseType.PropertyManagement)
            return;

        var availableFrom = property.AvailableFrom;
        var availableUntil = property.AvailableUntil;

        if (isMonthly)
            await CreateMonthlyJournalEntryForLinenAndTowelAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, processingDate, cancellationToken);
        else
            await CreateAnnualJournalEntryForLinenAndTowelAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, processingDate, cancellationToken);
    }

    private async Task CreateMonthlyJournalEntryForLinenAndTowelAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var monthStart = ResolveMonthStart(processingDate);
        var monthEnd = ResolveMonthEnd(processingDate);
        if (await HasLinensAndTowelsJournalEntryAsync(organizationId, agreement, monthStart, monthEnd))
            return;

        var wasRentedThisMonth = await _reservationRepository.WasRentedThisMonthAsync(agreement.PropertyId, organizationId, processingDate);
        if (!wasRentedThisMonth)
            return;

        await CreateJournalEntryForLinenAndTowelAgreementAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, isMonthly: true, processingDate, cancellationToken);
    }

    private async Task CreateAnnualJournalEntryForLinenAndTowelAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (processingDate.Month != 1 || processingDate.Day != 1)
            return;

        if (await HasLinensAndTowelsJournalEntryAsync(organizationId, agreement, processingDate, processingDate))
            return;

        await CreateJournalEntryForLinenAndTowelAgreementAsync(organizationId, agreement, propertyCode, availableFrom, availableUntil, isMonthly: false, processingDate, cancellationToken);
    }

    private async Task CreateJournalEntryForLinenAndTowelAgreementAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, DateOnly? availableFrom, DateOnly? availableUntil, bool isMonthly, DateOnly processingDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, agreement.OfficeId);
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

        var journalEntry = BuildJournalEntryFromLinenAndTowelAsync(organizationId, agreement, propertyCode, chartOfAccounts, accountingOffice, feeAmount, reverseEntryDirection, isMonthly, processingDate);
        await CreateAutoGeneratedJournalEntryAsync(journalEntry);
    }
    #endregion

    #region Journal Entries
    private JournalEntry BuildJournalEntryFromLinenAndTowelAsync(Guid organizationId, PropertyAgreement agreement, string propertyCode, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, decimal feeAmount, bool reverseEntryDirection, bool isMonthly, DateOnly processingDate)
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

        var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, agreement.OfficeId, accountingOffice);
        var linenAndTowelIncomeAccountId = GetDefaultLinenAndTowelIncome(chartOfAccounts, agreement.OfficeId, accountingOffice);
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

        return ClassifyJournalEntry(new JournalEntry
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
        },
            reverseEntryDirection ? JournalEntryKind.LinenTowelUnusedReversal : JournalEntryKind.LinenTowelFee,
            Perspective.Company);
    }
    #endregion

    #region Helpers
    private static DateOnly ResolveMonthStart(DateOnly processingDate) => new(processingDate.Year, processingDate.Month, 1);

    private static DateOnly ResolveMonthEnd(DateOnly processingDate) => ResolveMonthStart(processingDate).AddMonths(1).AddDays(-1);

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

    #endregion
}

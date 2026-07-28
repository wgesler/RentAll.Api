using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private sealed class CrossPeriodInvoiceAccountingContext
    {
        public required IReadOnlyList<ChartOfAccount> ChartOfAccounts { get; init; }
        public required AccountingOffice? AccountingOffice { get; init; }
        public required IReadOnlyDictionary<int, CostCode> CostCodeById { get; init; }
    }

    #region Cross-Period Invoice Journal Entries
    private async Task<(JournalEntry? Entry, string? DecisionMessage, bool PostAsStandardInvoice)> CreateJournalEntriesFromCrossPeriodInvoiceAsync(Invoice invoice, Guid currentUser)
    {
        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out var firstPeriodInvoice, out var secondPeriodInvoice))
            return (null, "Could not resolve cross-period date ranges from the rental fee ledger line.", false);

        var reservation = await _reservationRepository.GetReservationByIdAsync(invoice.ReservationId!.Value, invoice.OrganizationId);
        if (reservation == null)
            return (null, $"Reservation {invoice.ReservationId} was not found; cannot regenerate cross-period ledger lines.", false);

        if (!await TryPopulateCrossPeriodInvoiceLedgerLinesAsync(firstPeriodInvoice, reservation))
            return (null, $"Could not regenerate ledger lines for the first accounting period ({firstPeriodInvoice.AccountingPeriod:MM/yyyy}).", false);

        if (!await TryPopulateCrossPeriodInvoiceLedgerLinesAsync(secondPeriodInvoice, reservation))
            return (null, $"Could not regenerate ledger lines for the second accounting period ({secondPeriodInvoice.AccountingPeriod:MM/yyyy}).", false);

        if (TryGetNaFrequencyExtraFee(invoice, reservation, out var naFeeDescription))
            return (null, $"Extra fee '{naFeeDescription}' has an unsupported (NA) frequency; cannot determine how to split it across accounting periods.", false);

        var accountingContext = await LoadCrossPeriodInvoiceAccountingContextAsync(invoice);

        var apportionableIncomeLines = await GetApportionableIncomeChargeLinesAsync(invoice, reservation, accountingContext);
        if (!TryApplyApportionedCrossPeriodLinesFromOriginal(invoice, firstPeriodInvoice, secondPeriodInvoice, reservation, apportionableIncomeLines, accountingContext.CostCodeById))
            return (null, "Could not apportion the invoice charges across the two accounting periods.", false);

        var originalChargeTotal = SumInvoiceChargeLines(invoice, accountingContext.CostCodeById);
        var splitChargeTotal = SumInvoiceChargeLines(firstPeriodInvoice, accountingContext.CostCodeById)
            + SumInvoiceChargeLines(secondPeriodInvoice, accountingContext.CostCodeById);

        if (originalChargeTotal != splitChargeTotal)
        {
            var breakdown = BuildCrossPeriodChargeBreakdown(invoice, firstPeriodInvoice, secondPeriodInvoice, accountingContext.CostCodeById);
            return (null, $"Split charge total ({splitChargeTotal:0.00}) does not match the original invoice charge total ({originalChargeTotal:0.00}). {breakdown}", false);
        }

        var firstSliceChargeTotal = SumInvoiceChargeLines(firstPeriodInvoice, accountingContext.CostCodeById);
        var secondSliceChargeTotal = SumInvoiceChargeLines(secondPeriodInvoice, accountingContext.CostCodeById);
        if (firstSliceChargeTotal == 0 || secondSliceChargeTotal == 0)
        {
            // Every charge actually falls in a single accounting period (e.g. a nightly rental whose
            // checkout day is the 1st of the next month has 0 billable nights in that month). This is not
            // a real cross-period invoice; signal the caller to post it as one standard journal entry.
            return (null, "Both period slices have zero charges on at least one side; posting as a single journal entry.", true);
        }

        var chartOfAccounts = accountingContext.ChartOfAccounts.ToList();
        var allInvoiceEntries = await GetAllJournalEntriesForInvoiceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            invoice.InvoiceId);
        var firstExistingEntries = FilterInvoiceJournalEntriesForAccountingPeriod(
            allInvoiceEntries,
            firstPeriodInvoice.AccountingPeriod);
        var secondExistingEntries = FilterInvoiceJournalEntriesForAccountingPeriod(
            allInvoiceEntries,
            secondPeriodInvoice.AccountingPeriod);

        var firstEntry = await UpsertCrossPeriodSliceJournalEntryAsync(
            firstPeriodInvoice,
            invoice.InvoiceId,
            chartOfAccounts,
            accountingContext.AccountingOffice,
            firstExistingEntries,
            currentUser);

        var secondEntry = await UpsertCrossPeriodSliceJournalEntryAsync(
            secondPeriodInvoice,
            invoice.InvoiceId,
            chartOfAccounts,
            accountingContext.AccountingOffice,
            secondExistingEntries,
            currentUser);

        var retainedEntryIds = new HashSet<Guid>();
        if (firstEntry != null)
            retainedEntryIds.Add(firstEntry.JournalEntryId);
        if (secondEntry != null)
            retainedEntryIds.Add(secondEntry.JournalEntryId);

        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        if (invoice.LedgerLines.Any(l => l.Amount != 0 && IsCrossMonthRentalLine(l, referenceYear)))
        {
            if (TryGetInvoiceRentalLineAmount(firstPeriodInvoice, out _))
            {
                var firstRentPlus4000Base = await GetInvoiceRentPlus4000BaseAsync(firstPeriodInvoice);
                var firstOwnerShare = await UpsertJournalEntryFromInvoiceForOwnerShareAsync(
                    firstPeriodInvoice,
                    firstRentPlus4000Base,
                    firstExistingEntries,
                    currentUser);
                if (firstOwnerShare != null)
                    retainedEntryIds.Add(firstOwnerShare.JournalEntryId);
            }

            if (TryGetInvoiceRentalLineAmount(secondPeriodInvoice, out _))
            {
                var secondRentPlus4000Base = await GetInvoiceRentPlus4000BaseAsync(secondPeriodInvoice);
                var secondOwnerShare = await UpsertJournalEntryFromInvoiceForOwnerShareAsync(
                    secondPeriodInvoice,
                    secondRentPlus4000Base,
                    secondExistingEntries,
                    currentUser);
                if (secondOwnerShare != null)
                    retainedEntryIds.Add(secondOwnerShare.JournalEntryId);
            }
        }

        await DeleteJournalEntriesExceptAsync(
            firstExistingEntries.Where(IsInvoiceChargeOrOwnerExpectedJournalEntry),
            retainedEntryIds,
            invoice.OrganizationId);
        await DeleteJournalEntriesExceptAsync(
            secondExistingEntries.Where(IsInvoiceChargeOrOwnerExpectedJournalEntry),
            retainedEntryIds,
            invoice.OrganizationId);

        await LogInvoiceSplitDecisionAsync(invoice, split: true, firstPeriodInvoice, secondPeriodInvoice, message: "Cross-period split applied.");

        return (firstEntry, null, false);
    }

    private async Task<CrossPeriodInvoiceAccountingContext> LoadCrossPeriodInvoiceAccountingContextAsync(Invoice invoice)
    {
        var accountContextTask = LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodesTask = LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        await Task.WhenAll(accountContextTask, costCodesTask);

        var (chartOfAccounts, accountingOffice) = await accountContextTask;
        var costCodeById = await costCodesTask;

        return new CrossPeriodInvoiceAccountingContext
        {
            ChartOfAccounts = chartOfAccounts,
            AccountingOffice = accountingOffice,
            CostCodeById = costCodeById
        };
    }

    private async Task<JournalEntry?> CreateCrossPeriodSliceJournalEntryAsync(Invoice sliceInvoice, Guid sourceId, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser)
        => await UpsertCrossPeriodSliceJournalEntryAsync(sliceInvoice, sourceId, chartOfAccounts, accountingOffice, [], currentUser);

    private async Task<JournalEntry?> UpsertCrossPeriodSliceJournalEntryAsync(
        Invoice sliceInvoice,
        Guid sourceId,
        List<ChartOfAccount> chartOfAccounts,
        AccountingOffice? accountingOffice,
        IReadOnlyList<JournalEntry> existingSliceEntries,
        Guid currentUser)
    {
        var journalEntry = await CreateJournalEntryFromInvoiceAsync(
            sliceInvoice,
            chartOfAccounts,
            accountingOffice,
            currentUser,
            sourceId);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, sliceInvoice.OfficeId, accountingOffice);
        var chargeExisting = existingSliceEntries.FirstOrDefault(entry =>
            IsInvoiceChargeJournalEntry(entry, accountsReceivableAccountId)
            && MatchesJournalEntryAccountingPeriod(entry, sliceInvoice.AccountingPeriod));
        return await UpsertAutoGeneratedJournalEntryAsync(
            journalEntry,
            chargeExisting != null ? [chargeExisting] : [],
            currentUser,
            sliceInvoice.OrganizationId);
    }

    private async Task<Invoice> ResolveInvoiceForOwnerShareAsync(Invoice invoice)
    {
        if (!InvoiceCrossesAccountingPeriodBoundary(invoice) || invoice.AccountingPeriod == default)
            return invoice;

        if (!invoice.ReservationId.HasValue || invoice.ReservationId == Guid.Empty)
            return invoice;

        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out var firstPeriodInvoice, out var secondPeriodInvoice))
            return invoice;

        var sliceInvoice = firstPeriodInvoice.AccountingPeriod == invoice.AccountingPeriod
            ? firstPeriodInvoice
            : secondPeriodInvoice.AccountingPeriod == invoice.AccountingPeriod
                ? secondPeriodInvoice
                : null;

        if (sliceInvoice == null)
            return invoice;

        var reservation = await _reservationRepository.GetReservationByIdAsync(invoice.ReservationId.Value, invoice.OrganizationId);
        if (reservation == null)
            return invoice;

        if (TryGetNaFrequencyExtraFee(invoice, reservation, out _))
            return invoice;

        var accountingContext = await LoadCrossPeriodInvoiceAccountingContextAsync(invoice);
        var apportionableIncomeLines = await GetApportionableIncomeChargeLinesAsync(invoice, reservation, accountingContext);
        if (!TryApplyApportionedCrossPeriodLinesFromOriginal(
                invoice,
                firstPeriodInvoice,
                secondPeriodInvoice,
                reservation,
                apportionableIncomeLines,
                accountingContext.CostCodeById))
        {
            return invoice;
        }

        return sliceInvoice;
    }

    private async Task<bool> TryPopulateCrossPeriodInvoiceLedgerLinesAsync(Invoice sliceInvoice, Reservation reservation)
    {
        if (!TryParseInvoicePeriod(sliceInvoice.InvoicePeriod, out var periodStart, out var periodEnd))
            return false;

        var invoiceDate = sliceInvoice.InvoiceDate != default ? sliceInvoice.InvoiceDate : sliceInvoice.AccountingPeriod;
        var ledgerLines = await CreateLedgerLinesForReservationIdAsync(reservation, invoiceDate, periodStart, periodEnd);

        sliceInvoice.LedgerLines = ledgerLines;
        sliceInvoice.TotalAmount = ledgerLines.Sum(l => l.Amount);
        return true;
    }

    private static Dictionary<string, FrequencyType> BuildExtraFeeFrequencyByDescription(Reservation reservation)
    {
        // Extra fees are matched to invoice ledger lines by description (the cost code on the reservation
        // can drift away from what the invoice recorded, so it is not a reliable key). The ExtraFeeLine's
        // frequency is the authoritative signal for how the charge splits across accounting periods.
        var map = new Dictionary<string, FrequencyType>(StringComparer.Ordinal);
        foreach (var fee in reservation.ExtraFeeLines)
            map[fee.FeeDescription] = fee.FeeFrequency;
        return map;
    }

    private static IEnumerable<LedgerLine> GetSplitPoolExtraFeeLines(Invoice invoice, Reservation reservation)
    {
        // Daily and Monthly extra fees follow the tenant's days in each accounting period, exactly like
        // rent: Daily prorates naturally by day, Monthly splits by the day ratio. They join the rent
        // apportionment pool.
        var frequencyByDescription = BuildExtraFeeFrequencyByDescription(reservation);
        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (frequencyByDescription.TryGetValue(line.Description, out var frequency)
                && frequency is FrequencyType.Daily or FrequencyType.Monthly)
            {
                yield return line;
            }
        }
    }

    private static IEnumerable<LedgerLine> GetOccurrenceExtraFeeLines(Invoice invoice, Reservation reservation)
    {
        // Weekly (and rarer EOW/Quarterly/BiAnnually/Annually) extra fees are billed in the month each
        // occurrence falls, exactly like maid service.
        var frequencyByDescription = BuildExtraFeeFrequencyByDescription(reservation);
        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (frequencyByDescription.TryGetValue(line.Description, out var frequency)
                && IsOccurrenceFrequency(frequency))
            {
                yield return line;
            }
        }
    }

    private static bool IsOccurrenceFrequency(FrequencyType frequency)
        => frequency is FrequencyType.Weekly
            or FrequencyType.EOW
            or FrequencyType.Quarterly
            or FrequencyType.BiAnnually
            or FrequencyType.Annually;

    private static bool TryGetNaFrequencyExtraFee(Invoice invoice, Reservation reservation, out string description)
    {
        var frequencyByDescription = BuildExtraFeeFrequencyByDescription(reservation);
        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (frequencyByDescription.TryGetValue(line.Description, out var frequency)
                && frequency == FrequencyType.NA)
            {
                description = line.Description;
                return true;
            }
        }

        description = string.Empty;
        return false;
    }

    private async Task<List<LedgerLine>> GetApportionableIncomeChargeLinesAsync(Invoice invoice, Reservation reservation)
    {
        var accountingContext = await LoadCrossPeriodInvoiceAccountingContextAsync(invoice);
        return await GetApportionableIncomeChargeLinesAsync(invoice, reservation, accountingContext);
    }

    private Task<List<LedgerLine>> GetApportionableIncomeChargeLinesAsync(Invoice invoice, Reservation reservation, CrossPeriodInvoiceAccountingContext accountingContext)
    {
        // Charges that are NOT extra-fee lines are still classified by COST CODE: anything on a rental-income
        // code mapped under the 4000 parent account splits across both accounting periods like rent, everything else is a one-time
        // up-front charge. Extra-fee lines are excluded here because they are now routed by their frequency
        // instead. Rentals, maid service, and payments have their own dedicated handling.
        var extraFeeDescriptions = reservation.ExtraFeeLines
            .Select(f => f.FeeDescription)
            .ToHashSet(StringComparer.Ordinal);

        var matchedLines = new List<LedgerLine>();
        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (RentalFeePeriodRegex.IsMatch(line.Description.Trim()))
                continue;

            if (line.Description.StartsWith("Maid Service", StringComparison.Ordinal))
                continue;

            if (extraFeeDescriptions.Contains(line.Description))
                continue;

            accountingContext.CostCodeById.TryGetValue(line.CostCodeId, out var costCode);
            if (IsPaymentLedgerLine(costCode))
                continue;

            if (costCode != null && IsRentPlus4000CostCode(costCode, accountingContext.ChartOfAccounts, invoice.OfficeId))
                matchedLines.Add(line);
        }

        return Task.FromResult(matchedLines);
    }

    private async Task<decimal> SumInvoiceChargeLinesAsync(Invoice invoice)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        return SumInvoiceChargeLines(invoice, costCodeById);
    }

    private static decimal SumInvoiceChargeLines(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return invoice.LedgerLines
            .Where(l => l.Amount != 0)
            .Where(l =>
            {
                costCodeById.TryGetValue(l.CostCodeId, out var costCode);
                return !IsPaymentLedgerLine(costCode);
            })
            .Sum(l => l.Amount);
    }

    private async Task<string> BuildCrossPeriodChargeBreakdownAsync(Invoice original, Invoice firstSlice, Invoice secondSlice)
    {
        // Diagnostic detail for split mismatches: dump every non-payment charge line (description, amount,
        // cost code) for the original invoice and both regenerated period slices so the offending line is
        // obvious. The Message column is VARCHAR(2500), so each section is capped to stay within bounds.
        var costCodeById = await LoadCostCodeByOfficeIdAsync(original.OrganizationId, original.OfficeId);
        return BuildCrossPeriodChargeBreakdown(original, firstSlice, secondSlice, costCodeById);
    }

    private static string BuildCrossPeriodChargeBreakdown(Invoice original, Invoice firstSlice, Invoice secondSlice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {

        string FormatSection(string label, Invoice invoice)
        {
            var parts = invoice.LedgerLines
                .Where(l => l.Amount != 0)
                .Where(l =>
                {
                    costCodeById.TryGetValue(l.CostCodeId, out var costCode);
                    return !IsPaymentLedgerLine(costCode);
                })
                .Select(l =>
                {
                    costCodeById.TryGetValue(l.CostCodeId, out var costCode);
                    var code = costCode?.Code ?? "?";
                    return $"{l.Description}={l.Amount:0.00}[cc {code}]";
                });

            return $"{label}: {string.Join("; ", parts)}";
        }

        var message = string.Join(" || ",
            FormatSection("ORIGINAL", original),
            FormatSection($"P1 {firstSlice.AccountingPeriod:MM/yyyy}", firstSlice),
            FormatSection($"P2 {secondSlice.AccountingPeriod:MM/yyyy}", secondSlice));

        return message.Length > 2000 ? message[..2000] : message;
    }

    private async Task DeleteJournalEntriesForInvoiceChargesAsync(Invoice invoice)
    {
        await DeleteJournalEntriesForSourceByKindAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            SourceType.Invoice,
            invoice.InvoiceId,
            JournalEntryKind.Charge);

        await DeleteJournalEntriesForSourceByKindAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            SourceType.Invoice,
            invoice.InvoiceId,
            JournalEntryKind.OwnerExpected);

        await DeleteJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            (int)SourceType.OwnerDistribution,
            invoice.InvoiceId);
    }

    private async Task LogInvoiceSplitDecisionAsync(Invoice invoice, bool split, Invoice? firstPeriodInvoice = null, Invoice? secondPeriodInvoice = null, string? message = null)
    {
        var propertyId = await ResolveInvoicePropertyIdAsync(invoice);
        TryGetInvoiceRentalLedgerLine(invoice, out var rentalLine);
        var chargeTotal = await SumInvoiceChargeLinesAsync(invoice);
        var originalAmount = TryGetInvoiceRentalLineAmount(invoice, out var rentalAmount) ? rentalAmount : chargeTotal;

        decimal? firstAmount = null;
        decimal? secondAmount = null;
        string? firstPeriod = null;
        string? secondPeriod = null;

        if (split && firstPeriodInvoice != null && secondPeriodInvoice != null)
        {
            firstPeriod = firstPeriodInvoice.InvoicePeriod;
            secondPeriod = secondPeriodInvoice.InvoicePeriod;
            firstAmount = await SumInvoiceChargeLinesAsync(firstPeriodInvoice);
            secondAmount = await SumInvoiceChargeLinesAsync(secondPeriodInvoice);
        }
        else
        {
            firstPeriod = TryFormatBillingPeriodFromRental(invoice) ?? invoice.InvoicePeriod;
            firstAmount = chargeTotal;
        }

        await LogAccountingLogAsync(new AccountingLog
        {
            OrganizationId = invoice.OrganizationId,
            OfficeId = invoice.OfficeId,
            PropertyId = propertyId,
            InvoiceId = invoice.InvoiceId,
            OriginalAmount = originalAmount,
            RentalLine = rentalLine?.Description,
            Split = split,
            FirstPeriod = firstPeriod,
            SecondPeriod = secondPeriod,
            FirstAmount = firstAmount,
            SecondAmount = secondAmount,
            Message = message
        });
    }
    #endregion

    #region Cross-Period Invoice Journal Entry Static Helpers
    private static readonly Regex RentalFeePeriodRegex = new(@"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DescriptionPeriodRegex = new(@"\((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private async Task<bool> TryUseCrossPeriodInvoiceJournalEntryPathAsync(Invoice invoice)
    {
        if (!TryGetInvoiceRentalLedgerLine(invoice, out _))
            return false;

        if (!InvoiceCrossesAccountingPeriodBoundary(invoice))
            return false;

        if (!invoice.ReservationId.HasValue || invoice.ReservationId == Guid.Empty)
            return false;

        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out _, out _))
        {
            await LogInvoiceSplitDecisionAsync(invoice, split: false, message: "Could not resolve cross-period date ranges from the rental fee ledger line.");
            return false;
        }

        return true;
    }

    private static bool InvoiceCrossesAccountingPeriodBoundary(Invoice invoice)
        => TryGetPrimaryCrossMonthRentalDateRange(invoice, out _, out _);

    private static bool TryGetPrimaryCrossMonthRentalDateRange(Invoice invoice, out DateOnly rentalStart, out DateOnly rentalEnd)
    {
        rentalStart = default;
        rentalEnd = default;

        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (!TryParseRentalFeeDateRange(line.Description, referenceYear, out var start, out var end))
                continue;

            if (start.Year == end.Year && start.Month == end.Month)
                continue;

            rentalStart = start;
            rentalEnd = end;
            return true;
        }

        return false;
    }

    private static string? TryFormatBillingPeriodFromRental(Invoice invoice)
    {
        if (!TryGetPrimaryCrossMonthRentalDateRange(invoice, out var rentalStart, out var rentalEnd)
            && !TryGetAnyRentalDateRange(invoice, out rentalStart, out rentalEnd))
        {
            return null;
        }

        return FormatInvoicePeriod(rentalStart, rentalEnd);
    }

    private static bool TryGetAnyRentalDateRange(Invoice invoice, out DateOnly rentalStart, out DateOnly rentalEnd)
    {
        rentalStart = default;
        rentalEnd = default;

        if (!TryGetInvoiceRentalLedgerLine(invoice, out var rentalLine))
            return false;

        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        return TryParseRentalFeeDateRange(rentalLine.Description, referenceYear, out rentalStart, out rentalEnd);
    }

    private static bool TryGetInvoiceRentalLedgerLine(Invoice invoice, out LedgerLine rentalLine)
    {
        rentalLine = null!;
        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        foreach (var line in invoice.LedgerLines)
        {
            if (!TryParseRentalFeeDateRange(line.Description, referenceYear, out _, out _))
                continue;

            rentalLine = line;
            return true;
        }

        return false;
    }

    private static string BuildInvoiceJournalEntryMemo(Invoice invoice, IReadOnlyList<LedgerLine> chargeLines)
    {
        if (TryGetInvoiceRentalLedgerLine(invoice, out var rentalLine))
            return BuildInvoiceMemo(invoice.InvoiceCode, rentalLine.Description);

        var primaryChargeLine = chargeLines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line.Description));
        if (primaryChargeLine != null)
            return BuildInvoiceMemo(invoice.InvoiceCode, primaryChargeLine.Description);

        return BuildInvoiceMemo(invoice.InvoiceCode, "Charges");
    }

    private static bool TryGetInvoiceRentalLineAmount(Invoice invoice, out decimal rentalAmount)
    {
        rentalAmount = 0m;
        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        var rentalLines = invoice.LedgerLines
            .Where(l => l.Amount != 0)
            .Where(l => TryParseRentalFeeDateRange(l.Description, referenceYear, out _, out _))
            .ToList();
        if (rentalLines.Count == 0)
            return false;

        rentalAmount = rentalLines.Sum(l => l.Amount);
        return true;
    }

    private static bool TryParseInvoicePeriod(string? invoicePeriod, out DateOnly periodStart, out DateOnly periodEnd)
    {
        periodStart = default;
        periodEnd = default;

        if (string.IsNullOrWhiteSpace(invoicePeriod))
            return false;

        var parts = invoicePeriod.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!DateOnly.TryParse(parts[0], out periodStart) || !DateOnly.TryParse(parts[1], out periodEnd))
            return false;

        return periodEnd >= periodStart;
    }

    private static bool TryParseRentalFeeDateRange(string? description, int referenceYear, out DateOnly rentalStart, out DateOnly rentalEnd)
    {
        rentalStart = default;
        rentalEnd = default;

        if (string.IsNullOrWhiteSpace(description))
            return false;

        var match = RentalFeePeriodRegex.Match(description.Trim());
        if (!match.Success)
            return false;

        if (!TryParseMonthDay(match.Groups["start"].Value, referenceYear, out rentalStart))
            return false;

        if (!TryParseMonthDay(match.Groups["end"].Value, referenceYear, out rentalEnd))
            return false;

        if (rentalEnd < rentalStart)
            rentalEnd = rentalEnd.AddYears(1);

        return true;
    }

    private static bool TryParseDescriptionDateRange(string? description, int referenceYear, out DateOnly rangeStart, out DateOnly rangeEnd)
    {
        rangeStart = default;
        rangeEnd = default;

        if (string.IsNullOrWhiteSpace(description))
            return false;

        var match = DescriptionPeriodRegex.Match(description.Trim());
        if (!match.Success)
            return false;

        if (!TryParseMonthDay(match.Groups["start"].Value, referenceYear, out rangeStart))
            return false;

        if (!TryParseMonthDay(match.Groups["end"].Value, referenceYear, out rangeEnd))
            return false;

        if (rangeEnd < rangeStart)
            rangeEnd = rangeEnd.AddYears(1);

        return true;
    }

    private static bool TryParseMonthDay(string monthDay, int year, out DateOnly date)
    {
        date = default;
        var parts = monthDay.Split('/');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var day))
            return false;

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryApplyApportionedCrossPeriodLinesFromOriginal(Invoice originalInvoice, Invoice firstSlice, Invoice secondSlice, Reservation reservation, IReadOnlyList<LedgerLine> apportionableIncomeLines, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var referenceYear = originalInvoice.AccountingPeriod != default
            ? originalInvoice.AccountingPeriod.Year
            : originalInvoice.InvoiceDate.Year;

        var crossingRentals = originalInvoice.LedgerLines
            .Where(l => l.Amount != 0 && IsCrossMonthRentalLine(l, referenceYear))
            .ToList();

        if (crossingRentals.Count == 0)
            return true;

        var primaryRental = crossingRentals[0];
        if (!TryGetCrossMonthApportionmentDays(primaryRental, referenceYear, reservation, out var firstDays, out var totalDays))
            return false;

        if (!TryParseRentalFeeDateRange(primaryRental.Description, referenceYear, out var rentalStart, out var rentalEnd))
            return false;

        var firstMonthEnd = LastDayOfMonth(rentalStart);
        var secondMonthStart = FirstDayOfMonth(rentalEnd);
        var firstPeriodEnd = rentalEnd < firstMonthEnd ? rentalEnd : firstMonthEnd;
        var secondPeriodStart = rentalStart > secondMonthStart ? rentalStart : secondMonthStart;

        // Rebuild both accounting-period slices purely from the ORIGINAL invoice's charge lines. This
        // deliberately discards the reservation-regenerated seed so a slice can never carry a charge the
        // invoice did not actually have (e.g. a maid line that was never billed, or a fee whose cost code
        // drifted on the reservation). Every charge is re-added in apportioned form, so the two slices
        // always sum back to the original invoice.
        firstSlice.LedgerLines.Clear();
        secondSlice.LedgerLines.Clear();

        // Day-ratio pool (splits with the tenant's days, like rent): the crossing rental, eligible
        // ad-hoc/income lines whose description date range matches the rental range, and Daily/Monthly
        // extra fees.
        var rentalPeriodMatchedAdHocLines = GetRentalPeriodMatchedAdHocLines(
            originalInvoice,
            reservation,
            costCodeById,
            referenceYear,
            rentalStart,
            rentalEnd,
            primaryRental).ToList();
        var rentalRangeMatchedIncomeLines = apportionableIncomeLines
            .Where(line => TryParseDescriptionDateRange(line.Description, referenceYear, out var lineStart, out var lineEnd)
                && lineStart == rentalStart
                && lineEnd == rentalEnd)
            .ToList();
        var rentalPeriodMatchedLineKeys = rentalPeriodMatchedAdHocLines
            .Concat(rentalRangeMatchedIncomeLines)
            .Select(GetInvoiceLineKey)
            .ToHashSet(StringComparer.Ordinal);
        var pooledLines = crossingRentals
            .Cast<LedgerLine>()
            .Concat(rentalRangeMatchedIncomeLines)
            .Concat(GetSplitPoolExtraFeeLines(originalInvoice, reservation))
            .Concat(rentalPeriodMatchedAdHocLines)
            .GroupBy(GetInvoiceLineKey)
            .Select(g => g.First())
            .ToList();

        var totalPool = pooledLines.Sum(l => l.Amount);
        if (!TryApportionAmountByDayRatio(totalPool, firstDays, totalDays, out var firstPool, out var secondPool))
            return false;

        ApplyPooledMonthlyRecurringApportionment(
            firstSlice,
            secondSlice,
            pooledLines,
            firstPool,
            secondPool,
            totalPool,
            referenceYear,
            rentalStart,
            rentalEnd,
            firstPeriodEnd,
            secondPeriodStart);

        // One-time / up-front charges (deposits, pet, and OneTime extra fees) stay entirely on
        // the first accounting period. Platform departure fees bill on the last stay month instead.
        var oneTimeLines = GetOneTimeFeeLines(originalInvoice, reservation)
            .Where(line => !rentalPeriodMatchedLineKeys.Contains(GetInvoiceLineKey(line)))
            .ToList();
        foreach (var oneTimeLine in oneTimeLines)
        {
            var targetSlice = ShouldAssignDepartureFeeToSecondPeriod(oneTimeLine, reservation) ? secondSlice : firstSlice;
            targetSlice.LedgerLines.Add(CreateApportionedFeeLine(oneTimeLine, oneTimeLine.Amount));
        }

        // Occurrence-based extra fees (weekly, EOW, quarterly, ...) bill in the month each occurrence falls.
        var occurrenceExtraFeeLines = GetOccurrenceExtraFeeLines(originalInvoice, reservation)
            .Where(line => !rentalPeriodMatchedLineKeys.Contains(GetInvoiceLineKey(line)))
            .ToList();
        if (!TryApplyOccurrenceExtraFeesToCrossPeriodSlices(firstSlice, secondSlice, reservation, occurrenceExtraFeeLines))
            return false;

        // Maid service bills per visit in the month each visit occurs.
        var maidTemplate = originalInvoice.LedgerLines
            .FirstOrDefault(l => l.Amount != 0 && l.Description.StartsWith("Maid Service", StringComparison.Ordinal));
        if (!TryApplyMaidServiceToCrossPeriodSlices(
                firstSlice,
                secondSlice,
                reservation,
                rentalStart,
                rentalEnd,
                maidTemplate))
            return false;

        if (!TryApplyDatedOneTimeInvoiceChargeLinesToCrossPeriodSlices(
                originalInvoice,
                firstSlice,
                secondSlice,
                pooledLines,
                oneTimeLines,
                occurrenceExtraFeeLines,
                maidTemplate,
                costCodeById,
                referenceYear,
                rentalStart,
                rentalEnd,
                firstDays,
                totalDays))
            return false;

        firstSlice.TotalAmount = firstSlice.LedgerLines.Sum(l => l.Amount);
        secondSlice.TotalAmount = secondSlice.LedgerLines.Sum(l => l.Amount);
        return true;
    }

    private static IEnumerable<LedgerLine> GetRentalPeriodMatchedAdHocLines(Invoice invoice, Reservation reservation, IReadOnlyDictionary<int, CostCode> costCodeById, int referenceYear, DateOnly rentalStart, DateOnly rentalEnd, LedgerLine primaryRentalLine)
    {
        var extraFeeDescriptions = reservation.ExtraFeeLines
            .Select(f => f.FeeDescription)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (line.LedgerLineId == primaryRentalLine.LedgerLineId)
                continue;

            if (line.Description.StartsWith("Maid Service", StringComparison.Ordinal))
                continue;

            if (extraFeeDescriptions.Contains(line.Description))
                continue;

            costCodeById.TryGetValue(line.CostCodeId, out var costCode);
            if (IsPaymentLedgerLine(costCode))
                continue;

            if (!TryParseDescriptionDateRange(line.Description, referenceYear, out var lineStart, out var lineEnd))
                continue;

            if (lineStart == rentalStart && lineEnd == rentalEnd)
                yield return line;
        }
    }

    private static void ApplyPooledMonthlyRecurringApportionment(Invoice firstSlice, Invoice secondSlice, IReadOnlyList<LedgerLine> monthlyRecurringLines, decimal firstMonthPool, decimal secondMonthPool, decimal totalMonthlyRecurring, int referenceYear, DateOnly rentalStart, DateOnly rentalEnd, DateOnly firstPeriodEnd, DateOnly secondPeriodStart)
    {
        var distributedFirst = 0m;

        for (var i = 0; i < monthlyRecurringLines.Count; i++)
        {
            var line = monthlyRecurringLines[i];
            decimal firstAmount;
            decimal secondAmount;

            if (i == monthlyRecurringLines.Count - 1)
            {
                firstAmount = firstMonthPool - distributedFirst;
                secondAmount = line.Amount - firstAmount;
            }
            else
            {
                firstAmount = totalMonthlyRecurring == 0
                    ? 0
                    : Math.Round(firstMonthPool * line.Amount / totalMonthlyRecurring, 2, MidpointRounding.AwayFromZero);
                secondAmount = line.Amount - firstAmount;
                distributedFirst += firstAmount;
            }

            if (IsCrossMonthRentalLine(line, referenceYear))
            {
                firstSlice.LedgerLines.Add(CreateApportionedRentalLine(line, firstAmount, rentalStart, firstPeriodEnd));
                secondSlice.LedgerLines.Add(CreateApportionedRentalLine(line, secondAmount, secondPeriodStart, rentalEnd));
            }
            else
            {
                firstSlice.LedgerLines.Add(CreateApportionedFeeLine(line, firstAmount));
                secondSlice.LedgerLines.Add(CreateApportionedFeeLine(line, secondAmount));
            }
        }
    }

    private static bool TryApplyOccurrenceExtraFeesToCrossPeriodSlices(Invoice firstSlice, Invoice secondSlice, Reservation reservation, IReadOnlyList<LedgerLine> occurrenceLines)
    {
        if (occurrenceLines.Count == 0)
            return true;

        if (!TryGetSliceDateRange(firstSlice, out var slice1Start, out var slice1End))
            return false;

        if (!TryGetSliceDateRange(secondSlice, out var slice2Start, out var slice2End))
            return false;

        var billingPeriodStart = slice1Start;
        var billingPeriodEnd = slice2End;

        var frequencyByDescription = BuildExtraFeeFrequencyByDescription(reservation);

        foreach (var line in occurrenceLines)
        {
            if (!frequencyByDescription.TryGetValue(line.Description, out var frequency))
                return false;

            // Occurrences are anchored at the rental billing start, matching how the original line was
            // billed, then assigned to whichever accounting period each occurrence date falls in.
            var occurrences = GetScheduledOccurrenceDates(billingPeriodStart, billingPeriodStart, billingPeriodEnd, frequency);
            var totalOccurrences = occurrences.Count;
            if (totalOccurrences == 0)
                return false;

            var firstCount = occurrences.Count(d => d >= slice1Start && d <= slice1End);
            var secondCount = occurrences.Count(d => d >= slice2Start && d <= slice2End);
            if (firstCount + secondCount != totalOccurrences)
                return false;

            if (secondCount == 0)
            {
                firstSlice.LedgerLines.Add(CreateApportionedFeeLine(line, line.Amount));
            }
            else if (firstCount == 0)
            {
                secondSlice.LedgerLines.Add(CreateApportionedFeeLine(line, line.Amount));
            }
            else
            {
                var perOccurrence = line.Amount / totalOccurrences;
                var firstAmount = Math.Round(perOccurrence * firstCount, 2, MidpointRounding.AwayFromZero);
                var secondAmount = line.Amount - firstAmount;
                firstSlice.LedgerLines.Add(CreateApportionedFeeLine(line, firstAmount));
                secondSlice.LedgerLines.Add(CreateApportionedFeeLine(line, secondAmount));
            }
        }

        return true;
    }

    private static bool TryApplyMaidServiceToCrossPeriodSlices(Invoice firstSlice, Invoice secondSlice, Reservation reservation, DateOnly rentalStart, DateOnly rentalEnd, LedgerLine? maidTemplate)
    {
        if (maidTemplate == null)
            return true;

        if (!TryGetSliceDateRange(firstSlice, out var slice1Start, out var slice1End))
            return false;

        if (!TryGetSliceDateRange(secondSlice, out var slice2Start, out var slice2End))
            return false;

        if (!TryParseMaidServiceVisitCount(maidTemplate.Description, out var originalVisitCount))
            return false;

        if (!TryResolveBilledMaidServiceOccurrenceDates(
                reservation,
                slice1Start,
                slice2End,
                rentalStart,
                rentalEnd,
                originalVisitCount,
                out var occurrenceDates))
            return false;

        var slice1Count = occurrenceDates.Count(d => d >= slice1Start && d <= slice1End);
        var slice2Count = occurrenceDates.Count(d => d >= slice2Start && d <= slice2End);

        if (slice1Count + slice2Count != occurrenceDates.Count)
            return false;

        RemoveMaidServiceLines(firstSlice);
        RemoveMaidServiceLines(secondSlice);

        if (slice1Count > 0)
            firstSlice.LedgerLines.Add(CreateApportionedMaidServiceLine(maidTemplate, slice1Count, reservation.MaidServiceFee));

        if (slice2Count > 0)
            secondSlice.LedgerLines.Add(CreateApportionedMaidServiceLine(maidTemplate, slice2Count, reservation.MaidServiceFee));

        return true;
    }

    private static bool TryApplyDatedOneTimeInvoiceChargeLinesToCrossPeriodSlices(Invoice originalInvoice, Invoice firstSlice, Invoice secondSlice, IReadOnlyList<LedgerLine> pooledLines, IReadOnlyList<LedgerLine> oneTimeLines, IReadOnlyList<LedgerLine> occurrenceLines, LedgerLine? maidTemplate, IReadOnlyDictionary<int, CostCode> costCodeById, int referenceYear, DateOnly rentalStart, DateOnly rentalEnd, int firstDays, int totalDays)
    {
        if (!TryGetSliceDateRange(firstSlice, out var slice1Start, out var slice1End))
            return false;

        if (!TryGetSliceDateRange(secondSlice, out var slice2Start, out var slice2End))
            return false;

        var processedLineKeys = pooledLines
            .Concat(oneTimeLines)
            .Concat(occurrenceLines)
            .Select(GetInvoiceLineKey)
            .ToHashSet(StringComparer.Ordinal);

        if (maidTemplate != null)
            processedLineKeys.Add(GetInvoiceLineKey(maidTemplate));

        foreach (var line in originalInvoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (processedLineKeys.Contains(GetInvoiceLineKey(line)))
                continue;

            costCodeById.TryGetValue(line.CostCodeId, out var costCode);
            if (IsPaymentLedgerLine(costCode))
                continue;

            if (TryParseDescriptionDateRange(line.Description, referenceYear, out var lineStart, out var lineEnd)
                && lineStart == rentalStart
                && lineEnd == rentalEnd)
            {
                if (!TryApportionAmountByDayRatio(line.Amount, firstDays, totalDays, out var firstAmount, out var secondAmount))
                    return false;

                firstSlice.LedgerLines.Add(CreateApportionedFeeLine(line, firstAmount));
                secondSlice.LedgerLines.Add(CreateApportionedFeeLine(line, secondAmount));
                continue;
            }

            var effectiveLineDate = line.LedgerLineDate != default
                ? line.LedgerLineDate
                : originalInvoice.InvoiceDate;

            if (effectiveLineDate >= slice1Start && effectiveLineDate <= slice1End)
            {
                firstSlice.LedgerLines.Add(CreateApportionedFeeLine(line, line.Amount));
                continue;
            }

            if (effectiveLineDate >= slice2Start && effectiveLineDate <= slice2End)
            {
                secondSlice.LedgerLines.Add(CreateApportionedFeeLine(line, line.Amount));
                continue;
            }

            return false;
        }

        return true;
    }

    private static string GetInvoiceLineKey(LedgerLine line)
    {
        if (line.LedgerLineId != Guid.Empty)
            return line.LedgerLineId.ToString("D");

        return $"{line.LineNumber}|{line.CostCodeId}|{line.Amount:0.00}|{line.Description}|{line.LedgerLineDate:yyyy-MM-dd}";
    }

    private static bool TryResolveBilledMaidServiceOccurrenceDates(Reservation reservation, DateOnly invoicePeriodStart, DateOnly invoicePeriodEnd, DateOnly rentalStart, DateOnly rentalEnd, int originalVisitCount, out List<DateOnly> occurrenceDates)
    {
        occurrenceDates = [];

        var invoiceBounds = GetMaidServicePeriodBounds(reservation, invoicePeriodStart, invoicePeriodEnd);
        if (invoiceBounds.Start <= invoiceBounds.End)
        {
            var invoicePeriodDates = GetMaidServiceOccurrenceDates(reservation, invoiceBounds.Start, invoiceBounds.End);
            if (invoicePeriodDates.Count == originalVisitCount)
            {
                occurrenceDates = invoicePeriodDates;
                return true;
            }
        }

        var rentalBounds = GetMaidServicePeriodBounds(reservation, rentalStart, rentalEnd);
        if (rentalBounds.Start > rentalBounds.End)
            return originalVisitCount == 0;

        var rentalPeriodDates = GetMaidServiceOccurrenceDates(reservation, rentalBounds.Start, rentalBounds.End);
        if (rentalPeriodDates.Count != originalVisitCount)
            return false;

        occurrenceDates = rentalPeriodDates;
        return true;
    }

    private static (DateOnly Start, DateOnly End) GetMaidServicePeriodBounds(Reservation reservation, DateOnly periodStart, DateOnly periodEnd)
    {
        var startDate = reservation.MaidStartDate > periodStart ? reservation.MaidStartDate : periodStart;
        var endDate = periodEnd > reservation.DepartureDate.AddDays(-7) ? reservation.DepartureDate : periodEnd;
        return (startDate, endDate);
    }

    private static bool ShouldAssignDepartureFeeToSecondPeriod(LedgerLine line, Reservation reservation)
        => reservation.ReservationType == ReservationType.Platform
            && string.Equals(line.Description, "Departure Fee", StringComparison.Ordinal);

    private static IEnumerable<LedgerLine> GetOneTimeFeeLines(Invoice originalInvoice, Reservation reservation)
    {
        var oneTimeExtraDescriptions = reservation.ExtraFeeLines
            .Where(f => f.FeeFrequency == FrequencyType.OneTime)
            .Select(f => f.FeeDescription)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var line in originalInvoice.LedgerLines.Where(l => l.Amount != 0))
        {
            // Security Deposit Waiver is a deposit-type charge, not rental income, so it stays on the
            // first accounting period exactly like the Security Deposit instead of being apportioned.
            if (line.Description is "Departure Fee" or "Security Deposit" or "Security Deposit Waiver" or "Pet Fee")
            {
                yield return line;
                continue;
            }

            if (oneTimeExtraDescriptions.Contains(line.Description))
                yield return line;
        }
    }

    private static bool TryGetCrossMonthApportionmentDays(LedgerLine originalRental, int referenceYear, Reservation reservation, out int firstDays, out int totalDays)
    {
        firstDays = 0;
        totalDays = 0;

        if (!TryParseRentalFeeDateRange(originalRental.Description, referenceYear, out var rentalStart, out var rentalEnd))
            return false;

        if (rentalStart.Year == rentalEnd.Year && rentalStart.Month == rentalEnd.Month)
            return false;

        var departureDate = reservation.DepartureDate;
        totalDays = CalculateNumberOfDays(
            rentalStart,
            rentalEnd,
            reservation.BillingType,
            IsDepartureMonthYear(rentalEnd, departureDate),
            IsLastDayOfMonth(rentalEnd));

        if (totalDays <= 0)
            return false;

        var firstMonthEnd = LastDayOfMonth(rentalStart);
        var firstPeriodEnd = rentalEnd < firstMonthEnd ? rentalEnd : firstMonthEnd;

        firstDays = CalculateNumberOfDays(
            rentalStart,
            firstPeriodEnd,
            reservation.BillingType,
            IsDepartureMonthYear(firstPeriodEnd, departureDate),
            IsLastDayOfMonth(firstPeriodEnd));

        return firstDays > 0 && firstDays <= totalDays;
    }

    private static bool TryApportionAmountByDayRatio(decimal originalAmount, int firstDays, int totalDays, out decimal firstAmount, out decimal secondAmount)
    {
        firstAmount = 0;
        secondAmount = 0;

        if (totalDays <= 0 || firstDays < 0 || firstDays > totalDays)
            return false;

        var dailyRate = originalAmount / totalDays;
        firstAmount = Math.Round(dailyRate * firstDays, 2, MidpointRounding.AwayFromZero);
        secondAmount = originalAmount - firstAmount;
        return true;
    }

    private static bool IsCrossMonthRentalLine(LedgerLine line, int referenceYear)
    {
        if (!TryParseRentalFeeDateRange(line.Description, referenceYear, out var rentalStart, out var rentalEnd))
            return false;

        return rentalStart.Year != rentalEnd.Year || rentalStart.Month != rentalEnd.Month;
    }

    private static bool IsDepartureMonthYear(DateOnly date, DateOnly departureDate)
        => date.Year == departureDate.Year && date.Month == departureDate.Month;

    private static bool IsLastDayOfMonth(DateOnly date)
        => date == LastDayOfMonth(date);

    private static void RemoveMatchingFeeLine(Invoice slice, LedgerLine template)
        => slice.LedgerLines.RemoveAll(l =>
            l.Amount != 0
            && l.CostCodeId == template.CostCodeId
            && string.Equals(l.Description, template.Description, StringComparison.Ordinal));

    private static void RemoveGeneratedRentalFeeLines(Invoice slice)
        => slice.LedgerLines.RemoveAll(l => l.Amount != 0 && RentalFeePeriodRegex.IsMatch(l.Description.Trim()));

    private static void RemoveMaidServiceLines(Invoice slice)
        => slice.LedgerLines.RemoveAll(l => l.Amount != 0 && l.Description.StartsWith("Maid Service", StringComparison.Ordinal));

    private static bool TryParseMaidServiceVisitCount(string description, out int visitCount)
    {
        visitCount = 0;
        var open = description.IndexOf('(');
        var close = description.IndexOf(" times)", StringComparison.Ordinal);
        if (open < 0 || close <= open)
            return false;

        return int.TryParse(description.AsSpan(open + 1, close - open - 1), out visitCount);
    }

    private static LedgerLine CreateApportionedRentalLine(LedgerLine template, decimal amount, DateOnly start, DateOnly end)
        => new()
        {
            LedgerLineId = template.LedgerLineId,
            InvoiceId = template.InvoiceId,
            LineNumber = template.LineNumber,
            ReservationId = template.ReservationId,
            CostCodeId = template.CostCodeId,
            Amount = amount,
            Description = $"Rental Fee ({start:MM/dd}-{end:MM/dd})",
            LedgerLineDate = template.LedgerLineDate,
            CreatedOn = template.CreatedOn,
            CreatedBy = template.CreatedBy,
            ModifiedOn = template.ModifiedOn,
            ModifiedBy = template.ModifiedBy
        };

    private static LedgerLine CreateApportionedFeeLine(LedgerLine template, decimal amount)
        => new()
        {
            LedgerLineId = template.LedgerLineId,
            InvoiceId = template.InvoiceId,
            LineNumber = template.LineNumber,
            ReservationId = template.ReservationId,
            CostCodeId = template.CostCodeId,
            Amount = amount,
            Description = template.Description,
            LedgerLineDate = template.LedgerLineDate,
            CreatedOn = template.CreatedOn,
            CreatedBy = template.CreatedBy,
            ModifiedOn = template.ModifiedOn,
            ModifiedBy = template.ModifiedBy
        };

    private static LedgerLine CreateApportionedMaidServiceLine(LedgerLine template, int visitCount, decimal feePerVisit)
        => new()
        {
            LedgerLineId = template.LedgerLineId,
            InvoiceId = template.InvoiceId,
            LineNumber = template.LineNumber,
            ReservationId = template.ReservationId,
            CostCodeId = template.CostCodeId,
            Amount = visitCount * feePerVisit,
            Description = $"Maid Service ({visitCount} times)",
            LedgerLineDate = template.LedgerLineDate,
            CreatedOn = template.CreatedOn,
            CreatedBy = template.CreatedBy,
            ModifiedOn = template.ModifiedOn,
            ModifiedBy = template.ModifiedBy
        };

    private static bool TryCreateCrossPeriodInvoiceSlices(Invoice source, out Invoice firstPeriodInvoice, out Invoice secondPeriodInvoice)
    {
        firstPeriodInvoice = null!;
        secondPeriodInvoice = null!;

        if (!TryResolveCrossPeriodBounds(source, out var slice1Start, out var slice1End, out var slice2Start, out var slice2End))
            return false;

        var slice1AccountingPeriod = FirstDayOfMonth(slice1Start);
        var slice2AccountingPeriod = FirstDayOfMonth(slice2Start);

        firstPeriodInvoice = CloneInvoiceForCrossPeriodSlice(source, slice1Start, slice1End, slice1AccountingPeriod);
        secondPeriodInvoice = CloneInvoiceForCrossPeriodSlice(source, slice2Start, slice2End, slice2AccountingPeriod);
        return true;
    }

    private static bool TryResolveCrossPeriodBounds(Invoice invoice, out DateOnly slice1Start, out DateOnly slice1End, out DateOnly slice2Start, out DateOnly slice2End)
    {
        slice1Start = default;
        slice1End = default;
        slice2Start = default;
        slice2End = default;

        if (!TryGetPrimaryCrossMonthRentalDateRange(invoice, out var rentalStart, out var rentalEnd))
            return false;

        var firstMonthEnd = LastDayOfMonth(rentalStart);
        var secondMonthStart = FirstDayOfMonth(rentalEnd);

        slice1Start = rentalStart;
        slice1End = rentalEnd < firstMonthEnd ? rentalEnd : firstMonthEnd;
        slice2Start = rentalStart > secondMonthStart ? rentalStart : secondMonthStart;
        slice2End = rentalEnd;

        return slice1Start <= slice1End && slice2Start <= slice2End;
    }

    private static bool TryGetSliceDateRange(Invoice sliceInvoice, out DateOnly periodStart, out DateOnly periodEnd)
    {
        if (TryParseInvoicePeriod(sliceInvoice.InvoicePeriod, out periodStart, out periodEnd))
            return true;

        periodStart = default;
        periodEnd = default;
        return false;
    }

    private static Invoice CloneInvoiceForCrossPeriodSlice(Invoice source, DateOnly periodStart, DateOnly periodEnd, DateOnly accountingPeriod)
    {
        return new Invoice
        {
            InvoiceId = source.InvoiceId,
            OrganizationId = source.OrganizationId,
            OfficeId = source.OfficeId,
            OfficeName = source.OfficeName,
            InvoiceCode = source.InvoiceCode,
            ReservationId = source.ReservationId,
            ReservationCode = source.ReservationCode,
            PropertyId = source.PropertyId,
            PropertyCode = source.PropertyCode,
            ContactId = source.ContactId,
            ContactName = source.ContactName,
            ResponsibleParty = source.ResponsibleParty,
            InvoiceDate = source.InvoiceDate,
            DueDate = source.DueDate,
            AccountingPeriod = accountingPeriod,
            InvoicePeriod = FormatInvoicePeriod(periodStart, periodEnd),
            TotalAmount = 0,
            PaidAmount = source.PaidAmount,
            Notes = source.Notes,
            IsActive = source.IsActive,
            LedgerLines = new List<LedgerLine>(),
            CreatedOn = source.CreatedOn,
            CreatedBy = source.CreatedBy,
            ModifiedOn = source.ModifiedOn,
            ModifiedBy = source.ModifiedBy
        };
    }

    private static string FormatInvoicePeriod(DateOnly periodStart, DateOnly periodEnd)
        => $"{periodStart:MM/dd/yyyy} - {periodEnd:MM/dd/yyyy}";

    private static DateOnly FirstDayOfMonth(DateOnly date)
        => new(date.Year, date.Month, 1);

    private static DateOnly LastDayOfMonth(DateOnly date)
        => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    private static Guid GetInvoiceAccountingPeriodSourceId(Guid invoiceId, DateOnly accountingPeriod)
    {
        var input = $"{invoiceId:D}|{accountingPeriod:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
    #endregion
}

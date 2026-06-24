using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Cross-Period Invoice Journal Entries
    private async Task<(JournalEntry? Entry, string? DecisionMessage, bool PostAsStandardInvoice)> CreateJournalEntriesFromCrossPeriodInvoiceAsync(Invoice invoice, Guid currentUser)
    {
        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out var firstPeriodInvoice, out var secondPeriodInvoice))
            return (null, "Could not resolve the two accounting-period date ranges from the invoice period.", false);

        var reservation = await _reservationRepository.GetReservationByIdAsync(invoice.ReservationId!.Value, invoice.OrganizationId);
        if (reservation == null)
            return (null, $"Reservation {invoice.ReservationId} was not found; cannot regenerate cross-period ledger lines.", false);

        if (!await TryPopulateCrossPeriodInvoiceLedgerLinesAsync(firstPeriodInvoice, reservation))
            return (null, $"Could not regenerate ledger lines for the first accounting period ({firstPeriodInvoice.AccountingPeriod:MM/yyyy}).", false);

        if (!await TryPopulateCrossPeriodInvoiceLedgerLinesAsync(secondPeriodInvoice, reservation))
            return (null, $"Could not regenerate ledger lines for the second accounting period ({secondPeriodInvoice.AccountingPeriod:MM/yyyy}).", false);

        if (TryGetNaFrequencyExtraFee(invoice, reservation, out var naFeeDescription))
            return (null, $"Extra fee '{naFeeDescription}' has an unsupported (NA) frequency; cannot determine how to split it across accounting periods.", false);

        var apportionableIncomeLines = await GetApportionableIncomeChargeLinesAsync(invoice, reservation);
        if (!TryApplyApportionedCrossPeriodLinesFromOriginal(invoice, firstPeriodInvoice, secondPeriodInvoice, reservation, apportionableIncomeLines))
            return (null, "Could not apportion the invoice charges across the two accounting periods.", false);

        var originalChargeTotal = await SumInvoiceChargeLinesAsync(invoice);
        var splitChargeTotal = await SumInvoiceChargeLinesAsync(firstPeriodInvoice)
            + await SumInvoiceChargeLinesAsync(secondPeriodInvoice);

        if (originalChargeTotal != splitChargeTotal)
        {
            var breakdown = await BuildCrossPeriodChargeBreakdownAsync(invoice, firstPeriodInvoice, secondPeriodInvoice);
            return (null, $"Split charge total ({splitChargeTotal:0.00}) does not match the original invoice charge total ({originalChargeTotal:0.00}). {breakdown}", false);
        }

        var firstSliceChargeTotal = await SumInvoiceChargeLinesAsync(firstPeriodInvoice);
        var secondSliceChargeTotal = await SumInvoiceChargeLinesAsync(secondPeriodInvoice);
        if (firstSliceChargeTotal == 0 || secondSliceChargeTotal == 0)
        {
            // Every charge actually falls in a single accounting period (e.g. a nightly rental whose
            // checkout day is the 1st of the next month has 0 billable nights in that month). This is not
            // a real cross-period invoice; signal the caller to post it as one standard journal entry.
            return (null, "Both period slices have zero charges on at least one side; posting as a single journal entry.", true);
        }

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);

        var firstEntry = await CreateCrossPeriodSliceJournalEntryAsync(
            firstPeriodInvoice,
            invoice.InvoiceId,
            chartOfAccounts,
            accountingOffice,
            currentUser);

        await CreateCrossPeriodSliceJournalEntryAsync(
            secondPeriodInvoice,
            invoice.InvoiceId,
            chartOfAccounts,
            accountingOffice,
            currentUser,
            memoSuffix: secondPeriodInvoice.AccountingPeriod.ToString("MM/yyyy"));

        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        if (invoice.LedgerLines.Any(l => l.Amount != 0 && IsCrossMonthRentalLine(l, referenceYear)))
        {
            if (TryGetInvoiceRentalLineAmount(firstPeriodInvoice, out _))
            {
                var firstOwnerBase = await GetOwnerPercentageBaseAsync(firstPeriodInvoice);
                await CreateJournalEntryFromInvoiceOwnerShareAsync(firstPeriodInvoice, firstOwnerBase, currentUser);
            }

            if (TryGetInvoiceRentalLineAmount(secondPeriodInvoice, out _))
            {
                var secondOwnerBase = await GetOwnerPercentageBaseAsync(secondPeriodInvoice);
                await CreateJournalEntryFromInvoiceOwnerShareAsync(secondPeriodInvoice, secondOwnerBase, currentUser);
            }
        }

        await LogInvoiceSplitDecisionAsync(invoice, split: true, firstPeriodInvoice, secondPeriodInvoice, message: "Cross-period split applied.");

        return (firstEntry, null, false);
    }

    private async Task<JournalEntry?> CreateCrossPeriodSliceJournalEntryAsync(Invoice sliceInvoice, Guid sourceId, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser, string? memoSuffix = null)
    {
        var journalEntry = await BuildJournalEntryFromInvoiceAsync(
            sliceInvoice,
            chartOfAccounts,
            accountingOffice,
            currentUser,
            sourceId,
            memoSuffix);
        return await CreateAutoGeneratedJournalEntryAsync(journalEntry);
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
        // Charges that are NOT extra-fee lines are still classified by COST CODE: anything on a rental-income
        // code (4000-4999) splits across both accounting periods like rent, everything else is a one-time
        // up-front charge. Extra-fee lines are excluded here because they are now routed by their frequency
        // instead. Rentals, maid service, and payments have their own dedicated handling.
        var extraFeeDescriptions = reservation.ExtraFeeLines
            .Select(f => f.FeeDescription)
            .ToHashSet(StringComparer.Ordinal);

        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);

        var matchedLines = new List<LedgerLine>();
        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (RentalFeePeriodRegex.IsMatch(line.Description.Trim()))
                continue;

            if (line.Description.StartsWith("Maid Service", StringComparison.Ordinal))
                continue;

            if (extraFeeDescriptions.Contains(line.Description))
                continue;

            costCodeById.TryGetValue(line.CostCodeId, out var costCode);
            if (IsPaymentLedgerLine(costCode))
                continue;

            if (costCode != null && IsRentalIncomeCostCode(costCode))
                matchedLines.Add(line);
        }

        return matchedLines;
    }

    private static bool IsRentalIncomeCostCode(CostCode costCode)
    {
        var normalized = NormalizeAccountCode(costCode.Code);
        return int.TryParse(normalized, out var number) && number is >= 4000 and < 5000;
    }

    private async Task<decimal> SumInvoiceChargeLinesAsync(Invoice invoice)
    {
        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);

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
        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(original.OrganizationId, original.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);

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
        await DeleteJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            (int)SourceType.Invoice,
            invoice.InvoiceId);

        await DeleteJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            (int)SourceType.OwnerDistribution,
            invoice.InvoiceId);

        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out _, out var secondPeriodInvoice))
            return;

        var legacySecondSourceId = GetInvoiceAccountingPeriodSourceId(invoice.InvoiceId, secondPeriodInvoice.AccountingPeriod);
        await DeleteJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            (int)SourceType.Invoice,
            legacySecondSourceId);
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
            firstPeriod = invoice.InvoicePeriod;
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
    private static readonly Regex RentalFeePeriodRegex = new(
        @"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private async Task<bool> TryUseCrossPeriodInvoiceJournalEntryPathAsync(Invoice invoice)
    {
        if (!InvoiceCrossesAccountingPeriodBoundary(invoice))
        {
            await LogInvoiceSplitDecisionAsync(invoice, split: false, message: "Invoice does not cross an accounting period boundary.");
            return false;
        }

        if (!invoice.ReservationId.HasValue || invoice.ReservationId == Guid.Empty)
        {
            await LogInvoiceSplitDecisionAsync(invoice, split: false, message: "Invoice has no reservation; cross-period split requires a reservation.");
            return false;
        }

        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out _, out _))
        {
            await LogInvoiceSplitDecisionAsync(invoice, split: false, message: "Could not resolve the two accounting-period date ranges from the invoice period.");
            return false;
        }

        return true;
    }

    private static bool InvoiceCrossesAccountingPeriodBoundary(Invoice invoice)
    {
        if (TryParseInvoicePeriod(invoice.InvoicePeriod, out var periodStart, out var periodEnd)
            && (periodStart.Year != periodEnd.Year || periodStart.Month != periodEnd.Month))
        {
            return true;
        }

        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (!TryParseRentalFeeDateRange(line.Description, referenceYear, out var rentalStart, out var rentalEnd))
                continue;

            if (rentalStart.Year != rentalEnd.Year || rentalStart.Month != rentalEnd.Month)
                return true;
        }

        return false;
    }

    private static bool TryGetInvoiceRentalLedgerLine(Invoice invoice, out LedgerLine rentalLine)
    {
        rentalLine = null!;
        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (!TryParseRentalFeeDateRange(line.Description, referenceYear, out _, out _))
                continue;

            rentalLine = line;
            return true;
        }

        return false;
    }

    private static bool TryGetInvoiceRentalLineAmount(Invoice invoice, out decimal rentalAmount)
    {
        rentalAmount = 0m;
        if (!TryGetInvoiceRentalLedgerLine(invoice, out var rentalLine))
            return false;

        rentalAmount = rentalLine.Amount;
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

    private static bool TryApplyApportionedCrossPeriodLinesFromOriginal(Invoice originalInvoice, Invoice firstSlice, Invoice secondSlice, Reservation reservation, IReadOnlyList<LedgerLine> apportionableIncomeLines)
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

        // Day-ratio pool (splits with the tenant's days, like rent): the crossing rental, every non-extra
        // charge on a rental-income cost code (4000-4999), and Daily/Monthly extra fees.
        var pooledLines = crossingRentals
            .Cast<LedgerLine>()
            .Concat(apportionableIncomeLines)
            .Concat(GetSplitPoolExtraFeeLines(originalInvoice, reservation))
            .GroupBy(l => (l.CostCodeId, l.Description))
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

        // One-time / up-front charges (deposits, departure, pet, and OneTime extra fees) stay entirely on
        // the first accounting period.
        foreach (var oneTimeLine in GetOneTimeFeeLines(originalInvoice, reservation))
            firstSlice.LedgerLines.Add(CreateApportionedFeeLine(oneTimeLine, oneTimeLine.Amount));

        // Occurrence-based extra fees (weekly, EOW, quarterly, ...) bill in the month each occurrence falls.
        if (!TryApplyOccurrenceExtraFeesToCrossPeriodSlices(originalInvoice, firstSlice, secondSlice, reservation))
            return false;

        // Maid service bills per visit in the month each visit occurs.
        if (!TryApplyMaidServiceToCrossPeriodSlices(
                originalInvoice,
                firstSlice,
                secondSlice,
                reservation,
                rentalStart,
                rentalEnd))
            return false;

        firstSlice.TotalAmount = firstSlice.LedgerLines.Sum(l => l.Amount);
        secondSlice.TotalAmount = secondSlice.LedgerLines.Sum(l => l.Amount);
        return true;
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

    private static bool TryApplyOccurrenceExtraFeesToCrossPeriodSlices(Invoice originalInvoice, Invoice firstSlice, Invoice secondSlice, Reservation reservation)
    {
        var occurrenceLines = GetOccurrenceExtraFeeLines(originalInvoice, reservation).ToList();
        if (occurrenceLines.Count == 0)
            return true;

        if (!TryParseInvoicePeriod(originalInvoice.InvoicePeriod, out var periodStart, out var periodEnd))
            return false;

        if (!TryParseInvoicePeriod(firstSlice.InvoicePeriod, out var slice1Start, out var slice1End))
            return false;

        if (!TryParseInvoicePeriod(secondSlice.InvoicePeriod, out var slice2Start, out var slice2End))
            return false;

        var frequencyByDescription = BuildExtraFeeFrequencyByDescription(reservation);

        foreach (var line in occurrenceLines)
        {
            if (!frequencyByDescription.TryGetValue(line.Description, out var frequency))
                return false;

            // Occurrences are anchored at the invoice period start, matching how the original line was
            // billed, then assigned to whichever accounting period each occurrence date falls in.
            var occurrences = GetScheduledOccurrenceDates(periodStart, periodStart, periodEnd, frequency);
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

    private static bool TryApplyMaidServiceToCrossPeriodSlices(Invoice originalInvoice, Invoice firstSlice, Invoice secondSlice, Reservation reservation, DateOnly rentalStart, DateOnly rentalEnd)
    {
        var maidTemplate = originalInvoice.LedgerLines
            .FirstOrDefault(l => l.Amount != 0 && l.Description.StartsWith("Maid Service", StringComparison.Ordinal));
        if (maidTemplate == null)
            return true;

        if (!TryParseInvoicePeriod(originalInvoice.InvoicePeriod, out var invoicePeriodStart, out var invoicePeriodEnd))
            return false;

        if (!TryParseInvoicePeriod(firstSlice.InvoicePeriod, out var slice1Start, out var slice1End))
            return false;

        if (!TryParseInvoicePeriod(secondSlice.InvoicePeriod, out var slice2Start, out var slice2End))
            return false;

        if (!TryParseMaidServiceVisitCount(maidTemplate.Description, out var originalVisitCount))
            return false;

        if (!TryResolveBilledMaidServiceOccurrenceDates(
                reservation,
                invoicePeriodStart,
                invoicePeriodEnd,
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

        if (!TryParseInvoicePeriod(invoice.InvoicePeriod, out var periodStart, out var periodEnd))
            return false;

        slice1Start = periodStart;
        slice1End = LastDayOfMonth(periodStart);

        if (periodStart.Year != periodEnd.Year || periodStart.Month != periodEnd.Month)
        {
            slice2Start = FirstDayOfMonth(periodEnd);
            slice2End = periodEnd;
        }
        else if (TryGetCrossingRentalEndDate(invoice, out var rentalEnd))
        {
            slice2Start = FirstDayOfMonth(rentalEnd);
            slice2End = rentalEnd;
        }
        else
        {
            return false;
        }

        return slice1Start <= slice1End && slice2Start <= slice2End;
    }

    private static bool TryGetCrossingRentalEndDate(Invoice invoice, out DateOnly rentalEnd)
    {
        rentalEnd = default;
        var referenceYear = invoice.AccountingPeriod != default
            ? invoice.AccountingPeriod.Year
            : invoice.InvoiceDate.Year;

        var found = false;
        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            if (!TryParseRentalFeeDateRange(line.Description, referenceYear, out var rentalStart, out var endDate))
                continue;

            if (rentalStart.Year == endDate.Year && rentalStart.Month == endDate.Month)
                continue;

            if (!found || endDate > rentalEnd)
            {
                rentalEnd = endDate;
                found = true;
            }
        }

        return found;
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

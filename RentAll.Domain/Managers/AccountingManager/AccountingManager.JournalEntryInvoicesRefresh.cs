using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task RefreshInvoiceChargeJournalEntriesAsync(Invoice invoice, Guid currentUser)
    {
        if (!await IsAccountingFeatureEnabledAsync(invoice.OrganizationId))
        {
            await DeleteJournalEntriesForInvoiceChargesAsync(invoice);
            return;
        }

        if (await TryUseCrossPeriodInvoiceJournalEntryPathAsync(invoice))
        {
            var refreshed = await RefreshCrossPeriodInvoiceJournalEntriesAsync(invoice, currentUser);
            if (refreshed)
                return;

            // Never fall back to a single full-invoice Charge for a cross-period invoice — that posts
            // the whole amount on period 1 and can leave a stale period-2 slice (e.g. $5,950 + $2,385).
            await LogInvoiceSplitDecisionAsync(
                invoice,
                split: false,
                message: "Cross-period refresh failed; leaving existing charge journal entries unchanged.");
            return;
        }

        await RefreshStandardInvoiceChargeJournalEntriesAsync(invoice, currentUser);
    }

    private async Task<bool> RefreshCrossPeriodInvoiceJournalEntriesAsync(Invoice invoice, Guid currentUser)
    {
        if (!TryCreateCrossPeriodInvoiceSlices(invoice, out var firstPeriodInvoice, out var secondPeriodInvoice))
            return false;

        var reservation = await _reservationRepository.GetReservationByIdAsync(invoice.ReservationId!.Value, invoice.OrganizationId);
        if (reservation == null)
            return false;

        if (!await TryPopulateCrossPeriodInvoiceLedgerLinesAsync(firstPeriodInvoice, reservation)
            || !await TryPopulateCrossPeriodInvoiceLedgerLinesAsync(secondPeriodInvoice, reservation))
        {
            return false;
        }

        if (TryGetNaFrequencyExtraFee(invoice, reservation, out _))
            return false;

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
            return false;
        }

        var originalChargeTotal = SumInvoiceChargeLines(invoice, accountingContext.CostCodeById);
        var splitChargeTotal = SumInvoiceChargeLines(firstPeriodInvoice, accountingContext.CostCodeById)
            + SumInvoiceChargeLines(secondPeriodInvoice, accountingContext.CostCodeById);

        if (originalChargeTotal != splitChargeTotal)
            return false;

        var firstSliceChargeTotal = SumInvoiceChargeLines(firstPeriodInvoice, accountingContext.CostCodeById);
        var secondSliceChargeTotal = SumInvoiceChargeLines(secondPeriodInvoice, accountingContext.CostCodeById);
        if (firstSliceChargeTotal == 0 || secondSliceChargeTotal == 0)
            return false;

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

        if (invoice.LedgerLines.Any(line => line.Amount != 0 && IsCrossMonthRentalLine(line, referenceYear)))
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

        // Only prune Charge / OwnerExpected. Payment, PrePay, and OwnAct share SourceId=InvoiceId
        // and must survive charge refresh.
        await DeleteJournalEntriesExceptAsync(
            firstExistingEntries.Where(IsInvoiceChargeOrOwnerExpectedJournalEntry),
            retainedEntryIds,
            invoice.OrganizationId);
        await DeleteJournalEntriesExceptAsync(
            secondExistingEntries.Where(IsInvoiceChargeOrOwnerExpectedJournalEntry),
            retainedEntryIds,
            invoice.OrganizationId);

        await DeleteOwnerDistributionJournalEntriesForInvoiceAsync(invoice);
        await LogInvoiceSplitDecisionAsync(invoice, split: true, firstPeriodInvoice, secondPeriodInvoice, message: "Cross-period split applied.");
        return true;
    }

    private async Task RefreshStandardInvoiceChargeJournalEntriesAsync(Invoice invoice, Guid currentUser)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, invoice.OfficeId, accountingOffice);

        var allInvoiceEntries = await GetAllJournalEntriesForInvoiceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            invoice.InvoiceId);
        var existingInvoiceEntries = allInvoiceEntries
            .Where(entry => entry.JournalEntryKindId is JournalEntryKind.Charge or JournalEntryKind.OwnerExpected)
            .Where(entry => MatchesJournalEntryAccountingPeriod(entry, invoice.AccountingPeriod))
            .ToList();

        await DeleteOwnerDistributionJournalEntriesForInvoiceAsync(invoice);

        var chargeExisting = existingInvoiceEntries.FirstOrDefault(entry => IsInvoiceChargeJournalEntry(entry, accountsReceivableAccountId));

        var rebuiltCharge = await CreateJournalEntryFromInvoiceAsync(invoice, chartOfAccounts, accountingOffice, currentUser);
        var updatedCharge = await UpsertAutoGeneratedJournalEntryAsync(
            rebuiltCharge,
            chargeExisting != null ? [chargeExisting] : [],
            currentUser,
            invoice.OrganizationId);

        var retainedEntryIds = new HashSet<Guid>();
        if (updatedCharge != null)
            retainedEntryIds.Add(updatedCharge.JournalEntryId);

        JournalEntry? updatedOwnerShare = null;
        if (TryGetInvoiceRentalLineAmount(invoice, out _))
        {
            var rentPlus4000Base = await GetInvoiceRentPlus4000BaseAsync(invoice);
            updatedOwnerShare = await UpsertJournalEntryFromInvoiceForOwnerShareAsync(
                invoice,
                rentPlus4000Base,
                existingInvoiceEntries,
                currentUser);
        }
        else
        {
            var existingOwnerShare = existingInvoiceEntries.FirstOrDefault(IsInvoiceOwnerShareJournalEntry);
            if (existingOwnerShare != null)
                await DeleteOpenJournalEntryAsync(existingOwnerShare.JournalEntryId, invoice.OrganizationId);
        }

        if (updatedOwnerShare != null)
            retainedEntryIds.Add(updatedOwnerShare.JournalEntryId);

        await DeleteJournalEntriesExceptAsync(
            existingInvoiceEntries.Where(IsInvoiceChargeOrOwnerExpectedJournalEntry),
            retainedEntryIds,
            invoice.OrganizationId);
    }

    private async Task DeleteOwnerDistributionJournalEntriesForInvoiceAsync(Invoice invoice)
    {
        await DeleteJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            (int)SourceType.OwnerDistribution,
            invoice.InvoiceId);
    }

    private async Task<List<JournalEntry>> GetJournalEntriesForInvoicePaymentLedgerLineAsync(
        Guid organizationId,
        int officeId,
        Invoice invoice,
        LedgerLine paymentLedgerLine)
    {
        return (await GetAllJournalEntriesForInvoiceAsync(organizationId, officeId, invoice.InvoiceId))
            .Where(entry => IsInvoicePaymentLedgerLineJournalEntry(entry, invoice, paymentLedgerLine))
            .ToList();
    }
}

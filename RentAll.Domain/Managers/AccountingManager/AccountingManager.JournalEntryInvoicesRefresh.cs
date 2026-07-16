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
            await PersistInvoiceJournalEntryIdIfChangedAsync(invoice, null, currentUser);
            return;
        }

        if (await TryUseCrossPeriodInvoiceJournalEntryPathAsync(invoice))
        {
            var refreshed = await RefreshCrossPeriodInvoiceJournalEntriesAsync(invoice, currentUser);
            if (refreshed)
                return;

            await LogInvoiceSplitDecisionAsync(invoice, split: false, message: "Cross-period refresh failed; posting as a single journal entry.");
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
        var secondPeriodSourceId = GetInvoiceAccountingPeriodSourceId(invoice.InvoiceId, secondPeriodInvoice.AccountingPeriod);

        var firstExistingEntries = await GetJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            SourceType.Invoice,
            invoice.InvoiceId);
        var secondExistingEntries = await GetJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            SourceType.Invoice,
            secondPeriodSourceId);

        var firstEntry = await UpsertCrossPeriodSliceJournalEntryAsync(
            firstPeriodInvoice,
            invoice.InvoiceId,
            chartOfAccounts,
            accountingContext.AccountingOffice,
            firstExistingEntries,
            currentUser);

        var secondEntry = await UpsertCrossPeriodSliceJournalEntryAsync(
            secondPeriodInvoice,
            secondPeriodSourceId,
            chartOfAccounts,
            accountingContext.AccountingOffice,
            secondExistingEntries,
            currentUser);

        if (firstEntry != null)
            await PersistInvoiceJournalEntryIdIfChangedAsync(invoice, firstEntry.JournalEntryId, currentUser);

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

        await DeleteJournalEntriesExceptAsync(firstExistingEntries, retainedEntryIds, invoice.OrganizationId);
        await DeleteJournalEntriesExceptAsync(secondExistingEntries, retainedEntryIds, invoice.OrganizationId);

        await DeleteOwnerDistributionJournalEntriesForInvoiceAsync(invoice);
        await LogInvoiceSplitDecisionAsync(invoice, split: true, firstPeriodInvoice, secondPeriodInvoice, message: "Cross-period split applied.");
        return true;
    }

    private async Task RefreshStandardInvoiceChargeJournalEntriesAsync(Invoice invoice, Guid currentUser)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(invoice.OrganizationId, invoice.OfficeId);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, invoice.OfficeId, accountingOffice);

        var existingInvoiceEntries = await GetJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            SourceType.Invoice,
            invoice.InvoiceId);

        if (TryCreateCrossPeriodInvoiceSlices(invoice, out _, out var secondPeriodInvoice))
        {
            var secondPeriodSourceId = GetInvoiceAccountingPeriodSourceId(invoice.InvoiceId, secondPeriodInvoice.AccountingPeriod);
            var secondPeriodEntries = await GetJournalEntriesForSourceAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                SourceType.Invoice,
                secondPeriodSourceId);
            await DeleteJournalEntriesExceptAsync(secondPeriodEntries, Array.Empty<Guid>(), invoice.OrganizationId);
        }

        await DeleteOwnerDistributionJournalEntriesForInvoiceAsync(invoice);

        var chargeExisting = invoice.JournalEntryId is { } journalEntryId && journalEntryId != Guid.Empty
            ? existingInvoiceEntries.FirstOrDefault(entry => entry.JournalEntryId == journalEntryId)
            : existingInvoiceEntries.FirstOrDefault(entry => IsInvoiceChargeJournalEntry(entry, accountsReceivableAccountId));

        var rebuiltCharge = await CreateJournalEntryFromInvoiceAsync(invoice, chartOfAccounts, accountingOffice, currentUser);
        var updatedCharge = await UpsertAutoGeneratedJournalEntryAsync(
            rebuiltCharge,
            chargeExisting != null ? [chargeExisting] : [],
            currentUser,
            invoice.OrganizationId);

        if (updatedCharge != null)
            await PersistInvoiceJournalEntryIdIfChangedAsync(invoice, updatedCharge.JournalEntryId, currentUser);

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
                await DeleteJournalEntryAsync(existingOwnerShare.JournalEntryId, invoice.OrganizationId);
        }

        if (updatedOwnerShare != null)
            retainedEntryIds.Add(updatedOwnerShare.JournalEntryId);

        await DeleteJournalEntriesExceptAsync(existingInvoiceEntries, retainedEntryIds, invoice.OrganizationId);
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
        Guid ledgerLineId)
    {
        var paymentEntries = await GetJournalEntriesForSourceAsync(organizationId, officeId, SourceType.InvoicePayment, ledgerLineId);
        var applyEntries = await GetJournalEntriesForSourceAsync(organizationId, officeId, SourceType.Invoice, ledgerLineId);
        return paymentEntries
            .Concat(applyEntries)
            .GroupBy(entry => entry.JournalEntryId)
            .Select(group => group.First())
            .ToList();
    }
}

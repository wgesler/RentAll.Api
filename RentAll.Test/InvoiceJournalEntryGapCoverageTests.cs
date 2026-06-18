using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class InvoiceJournalEntryGapCoverageTests
{
    [Fact]
    public async Task DepartureFeeAndExtraFee_FirstMonthInvoice_JeBalancesAllChargeLines()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 7, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            departureFee: 175m,
            extraFeeLines:
            [
                new ExtraFeeLine
                {
                    FeeDescription = "Admin Fee",
                    FeeAmount = 95m,
                    FeeFrequency = FrequencyType.OneTime,
                    CostCodeId = AccountingManagerJournalEntryFeeTestSupport.ExtraFeeCostCodeId
                }
            ]);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Contains(invoice.LedgerLines, line => line.Description == "Departure Fee");
        Assert.Contains(invoice.LedgerLines, line => line.Description == "Admin Fee");
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task StandardPayment_OnInvoice_CreatesBalancedPaymentJe()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: 1500m,
            paymentDate: new DateOnly(2026, 4, 15));
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var paymentEntry = Assert.Single(context.ActiveJournalEntries, entry => entry.SourceTypeId == (int)SourceType.InvoicePayment);
        Assert.Equal(payment.LedgerLineId, paymentEntry.SourceId);
        AssertBalancedJournalEntry(paymentEntry);
        Assert.Equal(1500m, paymentEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId).Debit);
        Assert.Equal(1500m, paymentEntry.JournalEntryLines.Single(line => line.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal)).Credit);
    }

    [Fact]
    public async Task PrePayment_BeforeAccountingPeriod_CreatesReceivedAndApplyJes()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 2, 1);
        var periodEnd = new DateOnly(2026, 2, 28);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: 800m,
            paymentDate: new DateOnly(2026, 1, 25));
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var receivedEntry = Assert.Single(context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.InvoicePayment && entry.SourceId == payment.LedgerLineId);
        var applyEntry = Assert.Single(context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.Invoice && entry.SourceId == payment.LedgerLineId);

        AssertBalancedJournalEntry(receivedEntry);
        AssertBalancedJournalEntry(applyEntry);
        Assert.Equal(new DateOnly(2026, 1, 25), receivedEntry.TransactionDate);
        Assert.Equal(new DateOnly(2026, 2, 1), applyEntry.PostingDate);
        Assert.Equal(800m, receivedEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId).Credit);
        Assert.Equal(800m, applyEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId).Debit);
    }

    [Fact]
    public async Task UpdateInvoice_RefreshRecreatesSingleChargeJournalEntry()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var originalEntryId = context.ActiveJournalEntries.Single(entry => entry.SourceTypeId == (int)SourceType.Invoice).JournalEntryId;

        var rentalLine = invoice.LedgerLines.Single(line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Amount += 100m;
        invoice.TotalAmount += 100m;
        invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        await manager.UpdateInvoiceAsync(invoice);

        var chargeEntries = context.ActiveJournalEntries.Where(entry => entry.SourceTypeId == (int)SourceType.Invoice).ToList();
        Assert.Single(chargeEntries);
        Assert.NotEqual(originalEntryId, chargeEntries[0].JournalEntryId);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Theory]
    [InlineData("2026-04-30", "2026-05-31", "2026-04-01", "2026-04-30", "Rental Fee (04/30-04/30)", 100)]
    [InlineData("2026-04-01", "2026-05-31", "2026-04-01", "2026-04-30", "Rental Fee (04/01-04/30)", 3000)]
    [InlineData("2026-04-15", "2026-07-31", "2026-04-01", "2026-04-30", "Rental Fee (04/15-04/30)", 1600)]
    [InlineData("2026-04-10", "2026-04-20", "2026-04-01", "2026-04-30", "Rental Fee (04/10-04/20)", 1100)]
    public async Task FocusedEdgeCases_ThroughJePipeline_BalanceInvoice(
        string arrival,
        string departure,
        string periodStart,
        string periodEnd,
        string expectedRentalDescription,
        decimal expectedRentalAmount)
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            DateOnly.Parse(arrival),
            DateOnly.Parse(departure),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var start = DateOnly.Parse(periodStart);
        var end = DateOnly.Parse(periodEnd);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, start, end);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var rental = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.Equal(expectedRentalDescription, rental.Description);
        Assert.Equal(expectedRentalAmount, rental.Amount);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task ZeroTotalInvoice_CreatesJournalEntryWithoutActiveLines()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(reservation, periodStart, periodEnd, []);
        invoice.TotalAmount = 0;
        context.TrackInvoice(invoice);
        var manager = context.CreateManager();

        var journalEntry = await manager.CreateJournalEntryFromInvoiceAsync(
            invoice,
            AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.NotEqual(Guid.Empty, journalEntry.JournalEntryId);
        Assert.Equal((int)SourceType.Invoice, journalEntry.SourceTypeId);
        Assert.Equal(invoice.InvoiceId, journalEntry.SourceId);
        Assert.DoesNotContain(journalEntry.JournalEntryLines, line => line.Debit != 0 || line.Credit != 0);
    }

    [Fact]
    public async Task NetZeroInvoice_WithOffsettingChargeLines_CreatesBalancedJournalEntry()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(
            reservation,
            periodStart,
            periodEnd,
            [
                new LedgerLine { Description = "Rental Fee (04/01-04/30)", Amount = 100m, CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId },
                new LedgerLine { Description = "Rent Credit", Amount = -100m, CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId }
            ]);
        invoice.TotalAmount = 0;
        context.TrackInvoice(invoice);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntry = Assert.Single(context.ActiveJournalEntries, entry => entry.SourceTypeId == (int)SourceType.Invoice);
        var totalDebit = chargeEntry.JournalEntryLines.Sum(line => line.Debit);
        var totalCredit = chargeEntry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(totalDebit, totalCredit);
        Assert.Equal(100m, totalDebit);
        Assert.Equal(3, chargeEntry.JournalEntryLines.Count);
    }

    private static void AssertBalancedJournalEntry(JournalEntry journalEntry)
    {
        var totalDebit = journalEntry.JournalEntryLines.Sum(line => line.Debit);
        var totalCredit = journalEntry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(totalDebit, totalCredit);
    }
}

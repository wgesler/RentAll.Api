using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class InvoiceJournalEntryGapCoverageTests
{
    private static readonly DateOnly FebArrival = new(2026, 2, 4);
    private static readonly DateOnly FebMaidStart = new(2026, 2, 25);
    private static readonly DateOnly FebPeriodStart = new(2026, 2, 1);
    private static readonly DateOnly FebPeriodEnd = new(2026, 2, 28);

    private static readonly DateOnly JanArrival = new(2026, 1, 15);
    private static readonly DateOnly JanMaidStart = new(2026, 1, 22);
    private static readonly DateOnly JanFebPeriodStart = new(2026, 1, 15);
    private static readonly DateOnly JanFebPeriodEnd = new(2026, 2, 14);

    [Fact]
    public async Task CrossPeriodWithFees_FebInvoice_SplitsMaidServiceByOccurrenceMonth()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            FebMaidStart);

        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, FebPeriodStart, FebPeriodEnd);
        var manager = context.CreateManager();

        AssertMaidServiceCount(invoice.LedgerLines, expectedTimes: 2, feePerVisit: 100m);

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));
        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.Contains(chargeEntries[0].JournalEntryLines, line => line.Memo == "Maid Service (1 times)" && line.Credit == 100m);
        Assert.Contains(chargeEntries[1].JournalEntryLines, line => line.Memo == "Maid Service (1 times)" && line.Credit == 100m);
        Assert.Contains(chargeEntries[0].JournalEntryLines, line => line.Memo == "Security Deposit" && line.Credit == 500m);
        Assert.Contains(chargeEntries[0].JournalEntryLines, line => line.Memo == "Pet Fee" && line.Credit == 250m);
        Assert.DoesNotContain(chargeEntries[1].JournalEntryLines, line => line.Memo == "Security Deposit");
        Assert.DoesNotContain(chargeEntries[1].JournalEntryLines, line => line.Memo == "Pet Fee");
    }

    [Fact]
    public async Task CrossPeriodWithFees_JanFebInvoicePeriod_FallsBackToSingleJeThatBalancesFullInvoice()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            JanArrival,
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            JanMaidStart);

        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, JanFebPeriodStart, JanFebPeriodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodFallback,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));
        Assert.Single(context.ActiveJournalEntries.Where(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task SdwCrossMonthRental_FebInvoice_CrossPeriodSplitBalancesRentAndWaiver()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            hasPets: false);

        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, FebPeriodStart, FebPeriodEnd);
        var manager = context.CreateManager();

        Assert.Contains(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee (02/04-03/"));
        var originalSdw = Assert.Single(invoice.LedgerLines, line => line.Description == "Security Deposit Waiver");
        Assert.Equal(500m, originalSdw.Amount);

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));

        var sdwCredits = context.ActiveJournalEntries
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.CostCodeId == AccountingManagerJournalEntryFeeTestSupport.SdwCostCodeId)
            .Sum(line => line.Credit);

        Assert.Equal(originalSdw.Amount, sdwCredits);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task MonthlyExtraFeeCrossMonthRental_FebInvoice_CrossPeriodSplitApportionsFeeWithRent()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            depositType: DepositType.CLR,
            extraFeeLines:
            [
                new ExtraFeeLine
                {
                    FeeDescription = "Parking Fee",
                    FeeAmount = 300m,
                    FeeFrequency = FrequencyType.Monthly,
                    CostCodeId = AccountingManagerJournalEntryFeeTestSupport.ExtraFeeCostCodeId
                }
            ]);

        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, FebPeriodStart, FebPeriodEnd);
        var originalParking = Assert.Single(invoice.LedgerLines, line => line.Description == "Parking Fee");
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));

        var parkingCredits = context.ActiveJournalEntries
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.Memo == "Parking Fee")
            .Sum(line => line.Credit);

        Assert.Equal(originalParking.Amount, parkingCredits);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

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
        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Contains(invoice.LedgerLines, line => line.Description == "Departure Fee");
        Assert.Contains(invoice.LedgerLines, line => line.Description == "Admin Fee");
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task ApportionmentRounding_NightlyCrossMonthSplit_RentCreditsSumExactlyToOriginal()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 4, 5),
            new DateOnly(2026, 6, 15),
            ProrateType.SecondMonth,
            BillingType.Nightly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            depositType: DepositType.CLR);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var originalRental = Assert.Single(invoice.LedgerLines, line => line.Description == "Rental Fee (04/05-05/04)");
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));

        var rentalCredits = context.ActiveJournalEntries
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryTestSupport.TenantIncomeAccountId
                && line.Memo!.StartsWith("Rental Fee", StringComparison.Ordinal))
            .Sum(line => line.Credit);

        Assert.Equal(originalRental.Amount, rentalCredits);
        Assert.Equal(originalRental.Amount,
            context.ActiveJournalEntries.Sum(entry => entry.JournalEntryLines
                .Where(line => line.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
                .Sum(line => line.Debit)));
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
        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
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
        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
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
        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
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

    [Fact]
    public async Task UpdateInvoice_CrossMonthRentOnly_RefreshRecreatesTwoChargeJournalEntries()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            3000m);

        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, FebPeriodStart, FebPeriodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var originalEntryIds = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .Select(entry => entry.JournalEntryId)
            .ToHashSet();

        Assert.Equal(2, originalEntryIds.Count);

        var rentalLine = invoice.LedgerLines.Single(line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Amount += 30m;
        invoice.TotalAmount += 30m;
        invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        await manager.UpdateInvoiceAsync(invoice);

        var refreshedEntries = context.ActiveJournalEntries.Where(entry => entry.SourceTypeId == (int)SourceType.Invoice).ToList();
        Assert.Equal(2, refreshedEntries.Count);
        Assert.DoesNotContain(refreshedEntries, entry => originalEntryIds.Contains(entry.JournalEntryId));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(refreshedEntries, invoice);
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
        var (invoice, context) = await BuildTrackedFeeInvoiceAsync(reservation, start, end);
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

    private static async Task<(Invoice Invoice, AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context)> BuildTrackedFeeInvoiceAsync(
        Reservation reservation,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var manager = context.CreateManager();
        var ledgerLines = await AccountingManagerJournalEntryFeeTestSupport.GetInvoiceLedgerLinesAsync(manager, reservation, periodStart, periodEnd);
        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(reservation, periodStart, periodEnd, ledgerLines);
        context.TrackInvoice(invoice);
        return (invoice, context);
    }

    private static void AssertBalancedJournalEntry(JournalEntry journalEntry)
    {
        var totalDebit = journalEntry.JournalEntryLines.Sum(line => line.Debit);
        var totalCredit = journalEntry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(totalDebit, totalCredit);
    }

    private static void AssertMaidServiceCount(IEnumerable<LedgerLine> lines, int expectedTimes, decimal feePerVisit)
    {
        var maidLine = Assert.Single(lines, line => line.Description.StartsWith("Maid Service"));
        Assert.Equal($"Maid Service ({expectedTimes} times)", maidLine.Description);
        Assert.Equal(expectedTimes * feePerVisit, maidLine.Amount);
    }
}

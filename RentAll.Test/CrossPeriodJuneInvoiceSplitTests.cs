using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class CrossPeriodJuneInvoiceSplitTests
{
    private static readonly DateOnly JuneArrival = new(2026, 6, 21);
    private static readonly DateOnly JunePeriodStart = new(2026, 6, 1);
    private static readonly DateOnly JunePeriodEnd = new(2026, 6, 30);
    private static readonly DateOnly JulySliceStart = new(2026, 7, 1);
    private static readonly DateOnly JulySliceEnd = new(2026, 7, 20);

    [Fact]
    public void OriginalInvoice_IncludesCrossMonthRentSdwAndDepartureFee()
    {
        var reservation = CreateJuneReservation();
        var manager = CreateManager();
        var lines = GetLines(manager, reservation, JunePeriodStart, JunePeriodEnd);

        AssertLineAmount(lines, "Rental Fee (06/21-07/20)", 9000m);
        AssertLineAmount(lines, "Security Deposit Waiver", 60m);
        AssertLineAmount(lines, "Departure Fee", 500m);
        Assert.Equal(9560m, lines.Sum(l => l.Amount));
    }

    [Fact]
    public async Task CrossPeriodSplit_June21ToJuly20_AmortizesMonthlyChargesAndKeepsDepartureOnFirstMonth()
    {
        var reservation = CreateJuneReservation();
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var manager = context.CreateManager();
        var ledgerLines = await AccountingManagerJournalEntryFeeTestSupport.GetInvoiceLedgerLinesAsync(
            manager, reservation, JunePeriodStart, JunePeriodEnd);
        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(reservation, JunePeriodStart, JunePeriodEnd, ledgerLines);
        context.TrackInvoice(invoice);

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(e => e.SourceTypeId == (int)SourceType.Invoice));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);

        var chargeEntries = context.ActiveJournalEntries
            .Where(e => e.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(e => e.PostingDate)
            .ToList();

        var firstMonthTotal = chargeEntries[0].JournalEntryLines
            .Where(l => l.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
            .Sum(l => l.Debit);
        var secondMonthTotal = chargeEntries[1].JournalEntryLines
            .Where(l => l.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
            .Sum(l => l.Debit);

        Assert.Equal(3520m, firstMonthTotal);
        Assert.Equal(6040m, secondMonthTotal);

        var firstMonthIncome = chargeEntries[0].JournalEntryLines
            .Where(l => l.Credit > 0)
            .ToDictionary(l => l.Memo!, l => l.Credit);

        Assert.Equal(3000m, firstMonthIncome["Rental Fee (06/21-06/30)"]);
        Assert.Equal(20m, firstMonthIncome["Security Deposit Waiver"]);
        Assert.Equal(500m, firstMonthIncome["Departure Fee"]);

        var secondMonthIncome = chargeEntries[1].JournalEntryLines
            .Where(l => l.Credit > 0)
            .ToDictionary(l => l.Memo!, l => l.Credit);

        Assert.Equal(6000m, secondMonthIncome["Rental Fee (07/01-07/20)"]);
        Assert.Equal(40m, secondMonthIncome["Security Deposit Waiver"]);
        Assert.DoesNotContain(secondMonthIncome, kvp => kvp.Key == "Departure Fee");
    }

    [Fact]
    public void PooledMonthlyRecurring_June21ToJuly20_Amortizes9060OverThirtyDays()
    {
        var reservation = CreateJuneReservation();
        var manager = CreateManager();
        var originalLines = GetLines(manager, reservation, JunePeriodStart, JunePeriodEnd);

        var rental = Assert.Single(originalLines, l => l.Description == "Rental Fee (06/21-07/20)");
        var sdw = Assert.Single(originalLines, l => l.Description == "Security Deposit Waiver");
        var monthlyTotal = rental.Amount + sdw.Amount;

        Assert.Equal(9060m, monthlyTotal);

        const int totalDays = 30;
        const int juneDays = 10;
        const int julyDays = 20;

        Assert.Equal(3020m, monthlyTotal / totalDays * juneDays);
        Assert.Equal(6040m, monthlyTotal / totalDays * julyDays);
        Assert.Equal(3520m, monthlyTotal / totalDays * juneDays + 500m);
    }

    private static Reservation CreateJuneReservation()
        => AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            JuneArrival,
            new DateOnly(2026, 12, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            deposit: 60m,
            hasPets: false,
            departureFee: 500m,
            billingRate: 9000m);

    private static List<LedgerLine> GetLines(AccountingManager manager, Reservation reservation, DateOnly start, DateOnly end)
        => manager.GetLedgerLinesByReservationIdAsync(reservation, start, end, rentalCostCodeId: 77);

    private static void AssertLineAmount(IEnumerable<LedgerLine> lines, string description, decimal expectedAmount)
    {
        var line = Assert.Single(lines, l => l.Description == description);
        Assert.Equal(expectedAmount, line.Amount);
    }

    private static AccountingManager CreateManager()
        => AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class CrossPeriodInvoicePaymentTests
{
    private static readonly DateOnly JunePeriodStart = new(2026, 6, 1);
    private static readonly DateOnly JunePeriodEnd = new(2026, 6, 30);
    private static readonly DateOnly JuneAccountingPeriod = new(2026, 6, 1);
    private static readonly DateOnly JulyAccountingPeriod = new(2026, 7, 1);
    private static readonly DateOnly MayPaymentDate = new(2026, 5, 15);
    private static readonly DateOnly JunePaymentDate = new(2026, 6, 10);

    [Fact]
    public async Task CrossPeriodPayment_MayFullAmount_BothSlicesUsePrePaymentPath()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var payment = await ApplyPaymentAsync(scenario, MayPaymentDate, scenario.InvoiceTotal);
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(scenario.InvoiceTotal, scenario.JuneCharge, scenario.JulyCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(2, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(expectedJunePayment, SumPrePaymentApplyDebits(scenario.Context, payment, JuneAccountingPeriod));
        Assert.Equal(expectedJulyPayment, SumPrePaymentApplyDebits(scenario.Context, payment, JulyAccountingPeriod));
        Assert.Equal(scenario.InvoiceTotal, SumPrePaymentCredits(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, MayPaymentDate, scenario.InvoiceTotal);
        AssertBalancedJournalEntry(paymentEntry);
    }

    [Fact]
    public async Task CrossPeriodPayment_JuneFullAmount_JuneSliceNormalPayment_JulySlicePrePayment()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var payment = await ApplyPaymentAsync(scenario, JunePaymentDate, scenario.InvoiceTotal);
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(scenario.InvoiceTotal, scenario.JuneCharge, scenario.JulyCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(scenario.JuneCharge, expectedJunePayment);
        Assert.Equal(scenario.JulyCharge, expectedJulyPayment);

        Assert.Equal(1, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(expectedJulyPayment, SumPrePaymentApplyDebits(scenario.Context, payment, JulyAccountingPeriod));
        Assert.Equal(0m, SumPrePaymentApplyDebits(scenario.Context, payment, JuneAccountingPeriod));
        Assert.Equal(expectedJulyPayment, SumPrePaymentCredits(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, JunePaymentDate, scenario.InvoiceTotal);
        AssertBalancedJournalEntry(paymentEntry);
    }

    [Fact]
    public async Task CrossPeriodPayment_MayOverPayment_PaysBothSlicesThenLeavesUnappliedPrePayment()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var overPaymentAmount = scenario.InvoiceTotal + 500m;
        var payment = await ApplyPaymentAsync(scenario, MayPaymentDate, overPaymentAmount);
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(overPaymentAmount, scenario.JuneCharge, scenario.JulyCharge);

        Assert.Equal(500m, expectedUnapplied);
        Assert.Equal(2, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(expectedJunePayment, SumPrePaymentApplyDebits(scenario.Context, payment, JuneAccountingPeriod));
        Assert.Equal(expectedJulyPayment, SumPrePaymentApplyDebits(scenario.Context, payment, JulyAccountingPeriod));
        Assert.Equal(scenario.InvoiceTotal, SumPrePaymentApplyDebits(scenario.Context, payment, JuneAccountingPeriod) + SumPrePaymentApplyDebits(scenario.Context, payment, JulyAccountingPeriod));
        Assert.Equal(overPaymentAmount, SumPrePaymentCredits(scenario.Context, payment));
        Assert.Equal(expectedUnapplied, SumUnappliedPrePaymentCredits(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, MayPaymentDate, overPaymentAmount);
        AssertBalancedJournalEntry(paymentEntry);
    }

    [Fact]
    public async Task CrossPeriodPayment_JunePartialAmount_PaysFirstSliceThenPartialPrePaymentOnSecondSlice()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var partialPaymentAmount = 800m;
        var payment = await ApplyPaymentAsync(scenario, JunePaymentDate, partialPaymentAmount);
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(partialPaymentAmount, scenario.JuneCharge, scenario.JulyCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(scenario.JuneCharge, expectedJunePayment);
        Assert.Equal(300m, expectedJulyPayment);

        Assert.Equal(1, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(expectedJulyPayment, SumPrePaymentApplyDebits(scenario.Context, payment, JulyAccountingPeriod));
        Assert.Equal(0m, SumPrePaymentApplyDebits(scenario.Context, payment, JuneAccountingPeriod));
        Assert.Equal(expectedJulyPayment, SumPrePaymentCredits(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, JunePaymentDate, partialPaymentAmount);
        AssertBalancedJournalEntry(paymentEntry);
    }

    [Fact]
    public async Task CrossPeriodPayment_JulyFullAmount_BothSlicesUseNormalPaymentPath()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var payment = await ApplyPaymentAsync(scenario, new DateOnly(2026, 7, 20), scenario.InvoiceTotal);

        Assert.Equal(0, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(0, CountPrePaymentReceivedEntries(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, new DateOnly(2026, 7, 20), scenario.InvoiceTotal);
        AssertBalancedJournalEntry(paymentEntry);
    }

    [Fact]
    public async Task CrossPeriodPayment_MayUnderPayment_AppliesFullAmountToFirstSliceOnly()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var partialPaymentAmount = 400m;
        var payment = await ApplyPaymentAsync(scenario, MayPaymentDate, partialPaymentAmount);
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(partialPaymentAmount, scenario.JuneCharge, scenario.JulyCharge);

        Assert.Equal(partialPaymentAmount, expectedJunePayment);
        Assert.Equal(0m, expectedJulyPayment);
        Assert.Equal(0m, expectedUnapplied);

        Assert.Equal(1, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(partialPaymentAmount, SumPrePaymentApplyDebits(scenario.Context, payment, JuneAccountingPeriod));
        Assert.Equal(0m, SumPrePaymentApplyDebits(scenario.Context, payment, JulyAccountingPeriod));
        Assert.Equal(partialPaymentAmount, SumPrePaymentCredits(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, MayPaymentDate, partialPaymentAmount);
        AssertBalancedJournalEntry(paymentEntry);
    }

    private static async Task<CrossPeriodPaymentScenario> SetupCrossPeriodInvoice0626To0715Async()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 6, 26),
            new DateOnly(2026, 12, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.CLR,
            hasPets: false,
            departureFee: -1m,
            billingRate: 3000m);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            JunePeriodStart,
            JunePeriodEnd);

        var rentalLine = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Description = "Rental Fee (06/26-07/15)";
        rentalLine.Amount = 2000m;
        invoice.TotalAmount = invoice.LedgerLines.Where(line => line.Amount != 0).Sum(line => line.Amount);

        const decimal juneCharge = 500m;
        const decimal julyCharge = 1500m;
        Assert.Equal(juneCharge + julyCharge, invoice.TotalAmount);

        return new CrossPeriodPaymentScenario(invoice, context, juneCharge, julyCharge, invoice.TotalAmount);
    }

    private static async Task<LedgerLine> ApplyPaymentAsync(CrossPeriodPaymentScenario scenario, DateOnly paymentDate, decimal amount)
    {
        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            scenario.Invoice,
            amount: amount,
            paymentDate: paymentDate,
            description: "ACH - 06/26-07/15");
        scenario.Invoice.LedgerLines.Add(payment);

        await scenario.Context.CreateManager().CreateJournalEntryFromPaymentAsync(
            scenario.Invoice,
            payment,
            AccountingManagerJournalEntryTestSupport.CurrentUser);

        return payment;
    }

    private static (decimal FirstPaymentAmount, decimal SecondPaymentAmount, decimal UnappliedAmount) AllocatePaymentWaterfall(decimal paymentAmount, decimal firstChargeCap, decimal secondChargeCap)
    {
        var firstPaymentAmount = Math.Min(paymentAmount, firstChargeCap);
        var remainingAfterFirst = paymentAmount - firstPaymentAmount;
        var secondPaymentAmount = Math.Min(remainingAfterFirst, secondChargeCap);
        var unappliedAmount = remainingAfterFirst - secondPaymentAmount;
        return (firstPaymentAmount, secondPaymentAmount, unappliedAmount);
    }

    private static int CountPrePaymentApplyEntries(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment)
        => context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice
            && entry.SourceId == payment.LedgerLineId
            && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId && line.Debit > 0));

    private static int CountPrePaymentReceivedEntries(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment)
        => context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
            && entry.SourceId == payment.LedgerLineId
            && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId && line.Credit > 0));

    private static decimal SumPrePaymentApplyDebits(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment, DateOnly accountingPeriod)
        => context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice
                && entry.SourceId == payment.LedgerLineId
                && entry.TransactionDate == accountingPeriod)
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId)
            .Sum(line => line.Debit);

    private static decimal SumPrePaymentCredits(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment)
        => context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
                && entry.SourceId == payment.LedgerLineId)
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId)
            .Sum(line => line.Credit);

    private static decimal SumUnappliedPrePaymentCredits(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment)
    {
        var appliedAmount = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice
                && entry.SourceId == payment.LedgerLineId)
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId)
            .Sum(line => line.Debit);

        return SumPrePaymentCredits(context, payment) - appliedAmount;
    }

    private static JournalEntry AssertSingleFullPaymentEntry(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment, DateOnly paymentDate, decimal expectedAmount)
    {
        var paymentEntry = Assert.Single(GetStandardPaymentEntries(context, payment));
        Assert.Equal(paymentDate, paymentEntry.TransactionDate);
        Assert.Equal(expectedAmount, paymentEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId).Debit);
        Assert.Equal(expectedAmount, paymentEntry.JournalEntryLines.Single(line => line.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal)).Credit);
        return paymentEntry;
    }

    private static List<JournalEntry> GetStandardPaymentEntries(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, LedgerLine payment)
        => context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
                && entry.SourceId == payment.LedgerLineId
                && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId))
            .ToList();

    private static void AssertBalancedJournalEntry(JournalEntry journalEntry)
    {
        var totalDebit = journalEntry.JournalEntryLines.Sum(line => line.Debit);
        var totalCredit = journalEntry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(totalDebit, totalCredit);
    }

    private sealed record CrossPeriodPaymentScenario(
        Invoice Invoice,
        AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context,
        decimal JuneCharge,
        decimal JulyCharge,
        decimal InvoiceTotal);
}

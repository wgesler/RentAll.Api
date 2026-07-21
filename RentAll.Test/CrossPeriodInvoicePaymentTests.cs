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
    private static readonly DateOnly DecemberAccountingPeriod = new(2025, 12, 1);
    private static readonly DateOnly JanuaryAccountingPeriod = new(2026, 1, 1);
    private static readonly DateOnly DecemberPaymentDate = new(2025, 12, 1);

    [Fact]
    public async Task CrossPeriodPayment_MayFullAmount_BothSlicesUsePrePaymentPath()
    {
        var scenario = await SetupCrossPeriodInvoice0626To0715Async();
        var payment = await ApplyPaymentAsync(scenario, MayPaymentDate, scenario.InvoiceTotal);
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(scenario.InvoiceTotal, scenario.FirstPeriodCharge, scenario.SecondPeriodCharge);

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
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(scenario.InvoiceTotal, scenario.FirstPeriodCharge, scenario.SecondPeriodCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(scenario.FirstPeriodCharge, expectedJunePayment);
        Assert.Equal(scenario.SecondPeriodCharge, expectedJulyPayment);

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
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(overPaymentAmount, scenario.FirstPeriodCharge, scenario.SecondPeriodCharge);

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
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(partialPaymentAmount, scenario.FirstPeriodCharge, scenario.SecondPeriodCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(scenario.FirstPeriodCharge, expectedJunePayment);
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
        var (expectedJunePayment, expectedJulyPayment, expectedUnapplied) = AllocatePaymentWaterfall(partialPaymentAmount, scenario.FirstPeriodCharge, scenario.SecondPeriodCharge);

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

    /// <summary>
    /// Mirrors BWA24 / R-000026-001: rent 12/29-01/27, invoice period Dec, payment 12/1.
    /// Jan slice must get prepayment receive + apply (calendar-year boundary).
    /// </summary>
    [Fact]
    public async Task CrossPeriodPayment_DecemberPayment_YearBoundary_JanuarySliceGetsPrePaymentApply()
    {
        var scenario = await SetupCrossPeriodInvoice1229To0127Async(invoiceTotal: 2650m);
        var payment = await ApplyPaymentAsync(
            scenario,
            DecemberPaymentDate,
            scenario.InvoiceTotal,
            description: "CC (5295) 12/29-01/27");

        var (expectedDecemberPayment, expectedJanuaryPayment, expectedUnapplied) = AllocatePaymentWaterfall(
            scenario.InvoiceTotal,
            scenario.FirstPeriodCharge,
            scenario.SecondPeriodCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(265m, expectedDecemberPayment);
        Assert.Equal(2385m, expectedJanuaryPayment);

        Assert.Equal(1, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(1, CountPrePaymentReceivedEntries(scenario.Context, payment));
        Assert.Equal(0m, SumPrePaymentApplyDebits(scenario.Context, payment, DecemberAccountingPeriod));
        Assert.Equal(expectedJanuaryPayment, SumPrePaymentApplyDebits(scenario.Context, payment, JanuaryAccountingPeriod));
        Assert.Equal(expectedJanuaryPayment, SumPrePaymentCredits(scenario.Context, payment));

        var paymentEntry = AssertSingleFullPaymentEntry(scenario.Context, payment, DecemberPaymentDate, scenario.InvoiceTotal);
        AssertBalancedJournalEntry(paymentEntry);

        var januaryApply = Assert.Single(
            scenario.Context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.Invoice
                && entry.SourceId == payment.LedgerLineId
                && entry.TransactionDate == JanuaryAccountingPeriod);
        Assert.Equal(JanuaryAccountingPeriod, januaryApply.AccountingPeriod);
        AssertBalancedJournalEntry(januaryApply);
    }

    /// <summary>
    /// Same year-boundary stay with full invoice total matching production log
    /// 12/2025=595, 01/2026=5355 day-ratio of $5,950.
    /// </summary>
    [Fact]
    public async Task CrossPeriodPayment_DecemberPayment_YearBoundary_FullInvoiceTotal_JanuaryApplyUsesDayRatio()
    {
        var scenario = await SetupCrossPeriodInvoice1229To0127Async(invoiceTotal: 5950m);
        var payment = await ApplyPaymentAsync(
            scenario,
            DecemberPaymentDate,
            scenario.InvoiceTotal,
            description: "CC (5295) 12/29-01/27");

        var (expectedDecemberPayment, expectedJanuaryPayment, expectedUnapplied) = AllocatePaymentWaterfall(
            scenario.InvoiceTotal,
            scenario.FirstPeriodCharge,
            scenario.SecondPeriodCharge);

        Assert.Equal(0m, expectedUnapplied);
        Assert.Equal(595m, expectedDecemberPayment);
        Assert.Equal(5355m, expectedJanuaryPayment);

        Assert.Equal(1, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.Equal(expectedJanuaryPayment, SumPrePaymentApplyDebits(scenario.Context, payment, JanuaryAccountingPeriod));
        Assert.Equal(expectedJanuaryPayment, SumPrePaymentCredits(scenario.Context, payment));
    }

    /// <summary>
    /// Charge JEs first (P1 = invoice id, P2 = hashed year-boundary source id), then payment.
    /// </summary>
    [Fact]
    public async Task CrossPeriodPayment_YearBoundary_WithPostedChargeJes_CreatesJanuaryApply()
    {
        var scenario = await SetupCrossPeriodInvoice1229To0127Async(invoiceTotal: 2650m);
        var manager = scenario.Context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(scenario.Invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntries = scenario.Context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.TransactionDate)
            .ToList();
        Assert.Equal(2, chargeEntries.Count);
        Assert.Equal(scenario.Invoice.InvoiceId, chargeEntries[0].SourceId);
        Assert.NotEqual(scenario.Invoice.InvoiceId, chargeEntries[1].SourceId);
        Assert.Equal(DecemberAccountingPeriod, chargeEntries[0].TransactionDate);
        Assert.Equal(JanuaryAccountingPeriod, chargeEntries[1].TransactionDate);

        var payment = await ApplyPaymentAsync(
            scenario,
            DecemberPaymentDate,
            scenario.InvoiceTotal,
            description: "CC (5295) 12/29-01/27");

        Assert.Equal(1, CountPrePaymentApplyEntries(scenario.Context, payment));
        Assert.True(SumPrePaymentApplyDebits(scenario.Context, payment, JanuaryAccountingPeriod) > 0m);
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

    private static async Task<CrossPeriodPaymentScenario> SetupCrossPeriodInvoice1229To0127Async(decimal invoiceTotal)
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2025, 12, 29),
            new DateOnly(2026, 6, 30),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.CLR,
            hasPets: false,
            departureFee: -1m,
            billingRate: 3000m);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            DecemberAccountingPeriod,
            new DateOnly(2025, 12, 31));

        invoice.AccountingPeriod = DecemberAccountingPeriod;
        invoice.InvoiceDate = DecemberAccountingPeriod;
        invoice.DueDate = DecemberAccountingPeriod;

        var rentalLine = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Description = "Rental Fee (12/29-01/27)";
        rentalLine.Amount = invoiceTotal;
        rentalLine.LedgerLineDate = DecemberAccountingPeriod;
        invoice.LedgerLines = [rentalLine];
        invoice.TotalAmount = invoiceTotal;

        // Day-ratio fallback with no posted charge JEs: 3 Dec days / 27 Jan days.
        var firstPeriodCharge = Math.Round(invoiceTotal * (3m / 30m), 2, MidpointRounding.AwayFromZero);
        var secondPeriodCharge = invoiceTotal - firstPeriodCharge;

        return new CrossPeriodPaymentScenario(invoice, context, firstPeriodCharge, secondPeriodCharge, invoiceTotal);
    }

    private static async Task<LedgerLine> ApplyPaymentAsync(
        CrossPeriodPaymentScenario scenario,
        DateOnly paymentDate,
        decimal amount,
        string description = "ACH - 06/26-07/15")
    {
        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            scenario.Invoice,
            amount: amount,
            paymentDate: paymentDate,
            description: description);
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
        Assert.Equal(expectedAmount, paymentEntry.JournalEntryLines.Single(line => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(line.Memo)).Credit);
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
        decimal FirstPeriodCharge,
        decimal SecondPeriodCharge,
        decimal InvoiceTotal);
}

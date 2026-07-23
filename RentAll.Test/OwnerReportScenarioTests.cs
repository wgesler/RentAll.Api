namespace RentAll.Test;

public class OwnerReportScenarioTests
{
    private static readonly DateOnly MayStart = new(2026, 5, 1);
    private static readonly DateOnly MayEnd = new(2026, 5, 31);
    private static readonly DateOnly JuneStart = new(2026, 6, 1);
    private static readonly DateOnly JuneEnd = new(2026, 6, 30);
    private static readonly DateOnly MayJuneStart = MayStart;
    private static readonly DateOnly MayJuneEnd = JuneEnd;

    [Fact]
    public async Task LatePayment_MayOnly_ShowsUnpaidInvoiceLine()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetAccrualReportAsync(MayStart, MayEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent001, propertyRow.InvoicedIncome);
        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent001, propertyRow.OwnerProfit);

        var invoiceLine = ReportManagerTestSupport.FindActivityLine(
            report.PropertyActivityLines,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}");
        Assert.NotNull(invoiceLine);
        ReportManagerTestSupport.AssertActivityLine(
            invoiceLine!,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}",
            OwnerReportScenarioFixtures.OwnerRent001,
            0m);
        Assert.DoesNotContain(
            report.PropertyActivityLines,
            line => line.ReceivedIncome > 0);
    }

    [Fact]
    public async Task LatePayment_JuneOnly_ShowsJuneInvoiceUnpaidAndMayPaymentPaid()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetAccrualReportAsync(JuneStart, JuneEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent002, propertyRow.InvoicedIncome);
        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent002, propertyRow.OwnerProfit);

        var juneInvoiceLine = ReportManagerTestSupport.FindActivityLine(
            report.PropertyActivityLines,
            $"JE-{OwnerReportScenarioFixtures.Invoice002}");
        Assert.NotNull(juneInvoiceLine);
        ReportManagerTestSupport.AssertActivityLine(
            juneInvoiceLine!,
            $"JE-{OwnerReportScenarioFixtures.Invoice002}",
            OwnerReportScenarioFixtures.OwnerRent002,
            0m);

        var mayPaymentLine = ReportManagerTestSupport.FindActivityLine(
            report.PropertyActivityLines,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}");
        Assert.NotNull(mayPaymentLine);
        ReportManagerTestSupport.AssertActivityLine(
            mayPaymentLine!,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}",
            0m,
            OwnerReportScenarioFixtures.OwnerRent001);
        Assert.Equal(OwnerReportScenarioFixtures.JunePaymentDate, mayPaymentLine!.ActivityDate);
        Assert.Equal("06.26", mayPaymentLine.AccountingPeriod);
    }

    [Fact]
    public async Task LatePayment_MayAndJune_RollsUpMayInvoiceWithPayment()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetAccrualReportAsync(MayJuneStart, MayJuneEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(
            OwnerReportScenarioFixtures.OwnerRent001 + OwnerReportScenarioFixtures.OwnerRent002,
            propertyRow.InvoicedIncome);
        Assert.Equal(
            OwnerReportScenarioFixtures.OwnerRent001 + OwnerReportScenarioFixtures.OwnerRent002,
            propertyRow.OwnerProfit);

        var rolledUpLine = ReportManagerTestSupport.FindActivityLine(
            report.PropertyActivityLines,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}");
        Assert.NotNull(rolledUpLine);
        ReportManagerTestSupport.AssertActivityLine(
            rolledUpLine!,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}",
            OwnerReportScenarioFixtures.OwnerRent001,
            OwnerReportScenarioFixtures.OwnerRent001);

        var juneInvoiceLine = ReportManagerTestSupport.FindActivityLine(
            report.PropertyActivityLines,
            $"JE-{OwnerReportScenarioFixtures.Invoice002}");
        Assert.NotNull(juneInvoiceLine);
        ReportManagerTestSupport.AssertActivityLine(
            juneInvoiceLine!,
            $"JE-{OwnerReportScenarioFixtures.Invoice002}",
            OwnerReportScenarioFixtures.OwnerRent002,
            0m);
    }

    [Fact]
    public async Task LatePayment_JuneOnly_CashReportShowsOwnerPortionNotFullTenantPayment()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetCashReportAsync(JuneStart, JuneEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent001, propertyRow.ReceivedIncome);
        Assert.NotEqual(OwnerReportScenarioFixtures.TenantPayment001, propertyRow.ReceivedIncome);

        var paymentLine = ReportManagerTestSupport.FindActivityLine(
            report.PropertyActivityLines,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}");
        Assert.NotNull(paymentLine);
        ReportManagerTestSupport.AssertActivityLine(
            paymentLine!,
            $"JE-{OwnerReportScenarioFixtures.Invoice001}",
            0m,
            OwnerReportScenarioFixtures.OwnerRent001);
        Assert.Equal(
            OwnerReportScenarioFixtures.OwnerRent001,
            report.PropertyActivityLines.Sum(line => line.ReceivedIncome));
    }

    [Fact]
    public async Task Prepayment_MayOnly_CashReportShowsNoReceivedIncome()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildPrepaymentScenarioLines());

        var report = await context.GetCashReportAsync(MayStart, MayEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(0m, propertyRow.ReceivedIncome);
        Assert.Empty(report.PropertyActivityLines);
    }

    [Fact]
    public async Task Prepayment_MayOnly_AccrualReportShowsPrepaidLinesWithoutPaidIncome()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildPrepaymentScenarioLines());

        var report = await context.GetAccrualReportAsync(MayStart, MayEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(0m, propertyRow.InvoicedIncome);
        Assert.DoesNotContain(report.PropertyActivityLines, line => line.ReceivedIncome > 0);

        var prepaidLine = Assert.Single(report.PropertyActivityLines);
        Assert.Equal(2632m, prepaidLine.PrepaidIncome);
        Assert.Contains("R-00087-001", prepaidLine.Description, StringComparison.OrdinalIgnoreCase);
        ReportManagerTestSupport.AssertNoNegativePrepaidActivityLines(report.PropertyActivityLines);
    }

    [Fact]
    public async Task Prepayment_JuneOnly_ShowsPaidIncomeFromApplyNotReceiptMonth()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildPrepaymentScenarioLines());

        var accrualReport = await context.GetAccrualReportAsync(JuneStart, JuneEnd);
        var accrualRow = Assert.Single(accrualReport.Rows);

        Assert.Equal(49.70m, accrualRow.InvoicedIncome);

        var rolledUpLine = Assert.Single(accrualReport.PropertyActivityLines);
        ReportManagerTestSupport.AssertActivityLine(
            rolledUpLine,
            $"JE-{OwnerReportScenarioFixtures.CrossPeriodInvoice}",
            49.70m,
            49.70m);
        Assert.Equal(0m, rolledUpLine.PrepaidIncome);
        ReportManagerTestSupport.AssertNoNegativePrepaidActivityLines(accrualReport.PropertyActivityLines);

        var cashReport = await context.GetCashReportAsync(JuneStart, JuneEnd);
        var cashRow = Assert.Single(cashReport.Rows);
        Assert.Equal(49.70m, cashRow.ReceivedIncome);
    }

    [Fact]
    public async Task CrossPeriodPrepayment_JuneOnly_ShowsFirstSlicePaid()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildCrossPeriodPrepaymentScenarioLines());

        var report = await context.GetAccrualReportAsync(JuneStart, JuneEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(60m, propertyRow.InvoicedIncome);

        var juneLine = Assert.Single(report.PropertyActivityLines);
        ReportManagerTestSupport.AssertActivityLine(
            juneLine,
            $"JE-{OwnerReportScenarioFixtures.CrossPeriodInvoice}",
            60m,
            60m);
    }

    [Fact]
    public async Task CrossPeriodPrepayment_MayOnly_AccrualShowsPrepaidActivityWithoutInvoicedIncome()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildCrossPeriodPrepaymentScenarioLines());
        var report = await context.GetAccrualReportAsync(MayStart, MayEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(0m, propertyRow.InvoicedIncome);
        Assert.Contains(report.PropertyActivityLines, line => line.PrepaidIncome > 0);
    }

    [Fact]
    public async Task CrossPeriodPrepayment_MayOnly_CashAndAccrualExcludeReceiptMonthIncome()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildCrossPeriodPrepaymentScenarioLines());

        var cashReport = await context.GetCashReportAsync(MayStart, MayEnd);
        Assert.Equal(0m, Assert.Single(cashReport.Rows).ReceivedIncome);
        Assert.Empty(cashReport.PropertyActivityLines);

        var accrualReport = await context.GetAccrualReportAsync(MayStart, MayEnd);
        Assert.Contains(accrualReport.PropertyActivityLines, line => line.PrepaidIncome > 0);
        Assert.DoesNotContain(accrualReport.PropertyActivityLines, line => line.ReceivedIncome > 0);
    }

    [Fact]
    public async Task AccrualReport_OwnerProfit_IsInvoicedLessExpensesFlooredAtZero()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetAccrualReportAsync(MayStart, MayEnd);
        var propertyRow = Assert.Single(report.Rows);

        Assert.Equal(propertyRow.InvoicedIncome - propertyRow.OwnerExpenses, propertyRow.OwnerProfit);
        Assert.True(propertyRow.OwnerProfit >= 0m);
    }
}

namespace RentAll.Test;

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

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
    public async Task LatePayment_JuneOnly_ShowsJuneInvoiceOnly()
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

        Assert.DoesNotContain(
            report.PropertyActivityLines,
            line => string.Equals(line.DocumentCode, $"JE-{OwnerReportScenarioFixtures.Invoice001}", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.PropertyActivityLines, line => line.ReceivedIncome > 0);
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
            0m);

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
            0m);
        Assert.Equal(0m, rolledUpLine.PrepaidIncome);
        ReportManagerTestSupport.AssertNoNegativePrepaidActivityLines(accrualReport.PropertyActivityLines);

        var cashReport = await context.GetCashReportAsync(JuneStart, JuneEnd);
        var cashRow = Assert.Single(cashReport.Rows);
        Assert.Equal(49.70m, cashRow.ReceivedIncome);
    }

    [Fact]
    public async Task Buck805Prepayment_JuneOnly_CashIncludesPrepaidInvoiceSliceByRef()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildBuck805JuneScenarioLines());

        var cashReport = await context.GetCashReportAsync(JuneStart, JuneEnd);
        var cashRow = Assert.Single(cashReport.Rows);

        Assert.Equal(
            OwnerReportScenarioFixtures.Buck805OwnerRent001
                + OwnerReportScenarioFixtures.Buck805OwnerRent002
                + OwnerReportScenarioFixtures.Buck805OwnerRent003,
            cashRow.ReceivedIncome);

        var prepaidLine = cashReport.PropertyActivityLines.FirstOrDefault(
            line => string.Equals(line.SourceDocumentCode, OwnerReportScenarioFixtures.Buck805Invoice001, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(prepaidLine);
        Assert.Equal(OwnerReportScenarioFixtures.Buck805OwnerRent001, prepaidLine!.ReceivedIncome);
        Assert.Equal(0m, prepaidLine.ExpectedIncome);
    }

    [Fact]
    public async Task Buck805Prepayment_JuneOnly_AccrualShowsAllInvoicedSlices()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildBuck805JuneScenarioLines());

        var accrualReport = await context.GetAccrualReportAsync(JuneStart, JuneEnd);
        var accrualRow = Assert.Single(accrualReport.Rows);

        Assert.Equal(
            OwnerReportScenarioFixtures.Buck805OwnerRent001
                + OwnerReportScenarioFixtures.Buck805OwnerRent002
                + OwnerReportScenarioFixtures.Buck805OwnerRent003,
            accrualRow.InvoicedIncome);
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
            0m);
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

    [Fact]
    public async Task CashReport_NegativeVendorCredit_RemainsExpenseActivity()
    {
        var lines = new List<JournalEntryRecapLine>
        {
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                100m,
                OwnerReportScenarioFixtures.Invoice001,
                JuneStart,
                JuneStart,
                $"{OwnerReportScenarioFixtures.Invoice001}: Owner: Actual: Rent"),
            ReportManagerTestSupport.RecapLine(
                "Payment",
                100m,
                OwnerReportScenarioFixtures.Invoice001,
                JuneStart,
                JuneStart),
            ReportManagerTestSupport.RecapLine(
                "Expense",
                -35m,
                "RC-000001",
                JuneStart,
                JuneStart,
                "RC-000001: Owner: City of Littleton - Water (credit)",
                sourceTypeId: (int)SourceType.Receipt)
        };
        var context = ReportManagerTestSupport.CreateContext(lines);

        var report = await context.GetCashReportAsync(JuneStart, JuneEnd);

        var creditLine = ReportManagerTestSupport.FindActivityLine(report.PropertyActivityLines, "JE-RC-000001");
        Assert.NotNull(creditLine);
        Assert.Equal(-35m, creditLine!.Expenses);
        Assert.Equal(0m, creditLine.OwnerPayment);
    }

    [Fact]
    public async Task CashReport_LegacyOwnerPaymentMemo_IsNotExpenseActivity()
    {
        var lines = new List<JournalEntryRecapLine>
        {
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                100m,
                OwnerReportScenarioFixtures.Invoice001,
                JuneStart,
                JuneStart,
                $"{OwnerReportScenarioFixtures.Invoice001}: Owner: Actual: Rent"),
            ReportManagerTestSupport.RecapLine(
                "Payment",
                100m,
                OwnerReportScenarioFixtures.Invoice001,
                JuneStart,
                JuneStart),
            ReportManagerTestSupport.RecapLine(
                "Expense",
                -2649.32m,
                "OWNER-PAY-JUNE",
                JuneStart,
                JuneStart,
                "Owner's Payments - June 2026"),
            ReportManagerTestSupport.RecapLine(
                "OwnerPayment",
                2649.32m,
                "P-000001",
                JuneStart,
                JuneEnd,
                "DRE105: Owner: Payment: ACH",
                sourceTypeId: (int)SourceType.OwnerDistribution)
        };
        var context = ReportManagerTestSupport.CreateContext(lines);

        var report = await context.GetCashReportAsync(JuneStart, JuneEnd);

        Assert.DoesNotContain(
            report.PropertyActivityLines,
            line => line.Expenses != 0 && line.Description.Contains("Owner's Payments", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.PropertyActivityLines, line => line.OwnerPayment == 2649.32m);
    }

    [Fact]
    public async Task CashReport_GrossOwnerActual_ShowsCreditAsIncomeAndEachFeeDebitAsACharge()
    {
        var november = new DateOnly(2026, 11, 1);
        var novemberEnd = new DateOnly(2026, 11, 30);
        const string invoice = "R-000000173-001";
        var lines = new List<JournalEntryRecapLine>
        {
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                6754m,
                invoice,
                november,
                november,
                "R-000000173-001: Owner: Expected: Rental Fee (11/01-11/30)",
                journalEntryKindId: (int)JournalEntryKind.OwnerExpected,
                irsMethodTypeId: (int)IRSMethodType.IrsGrossMethod,
                credit: 6754m),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                -834m,
                invoice,
                november,
                november,
                "R-000000173-001: Owner: Expected: Rental Fee (11/01-11/30) (12%)",
                journalEntryKindId: (int)JournalEntryKind.OwnerExpected,
                irsMethodTypeId: (int)IRSMethodType.IrsGrossMethod,
                debit: 834m),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                -45m,
                invoice,
                november,
                november,
                "R-000000173-001: Owner: Expected: Departure Fee (12%)",
                journalEntryKindId: (int)JournalEntryKind.OwnerExpected,
                irsMethodTypeId: (int)IRSMethodType.IrsGrossMethod,
                debit: 45m),
            GrossOwnerActualLine(invoice, november, 7675m, 0m, 7675m, "R-000000173-001: Owner: Actual: Rental Fee (11/01-11/30) (ACH)"),
            GrossOwnerActualLine(invoice, november, -834m, 834m, 0m, "R-000000173-001: Owner: Actual: Rental Fee (11/01-11/30) (12%)"),
            GrossOwnerActualLine(invoice, november, -42m, 42m, 0m, "R-000000173-001: Owner: Actual: Pet Fee (12%)"),
            GrossOwnerActualLine(invoice, november, -45m, 45m, 0m, "R-000000173-001: Owner: Actual: Departure Fee (12%)")
        };
        var context = ReportManagerTestSupport.CreateContext(lines);

        var report = await context.GetCashReportAsync(november, novemberEnd);
        var row = Assert.Single(report.Rows);

        Assert.Equal(7675m, row.ReceivedIncome);
        Assert.Equal(921m, row.OwnerExpenses);
        Assert.Equal(6754m, row.OwnerPayment);

        var income = Assert.Single(report.PropertyActivityLines, line => line.ReceivedIncome != 0);
        Assert.Equal(7675m, income.ReceivedIncome);
        Assert.Equal("Rental Fee (11/01-11/30)", income.Description);

        var charges = report.PropertyActivityLines
            .Where(line => line.Expenses != 0)
            .OrderBy(line => line.Expenses)
            .ToList();
        Assert.Equal(3, charges.Count);
        Assert.Equal(42m, charges[0].Expenses);
        Assert.Equal("Pet Fee (12%)", charges[0].Description);
        Assert.Equal(45m, charges[1].Expenses);
        Assert.Equal("Departure Fee (12%)", charges[1].Description);
        Assert.Equal(834m, charges[2].Expenses);
        Assert.Equal("Rental Fee (11/01-11/30) (12%)", charges[2].Description);
    }

    private static JournalEntryRecapLine GrossOwnerActualLine(
        string invoice,
        DateOnly november,
        decimal amount,
        decimal debit,
        decimal credit,
        string description)
    {
        return ReportManagerTestSupport.RecapLine(
            "OwnerRentActual",
            amount,
            invoice,
            november,
            november,
            description,
            chartOfAccountId: ReportManagerTestSupport.OwnerAccountsPayableAccountId,
            journalEntryKindId: (int)JournalEntryKind.OwnerActual,
            irsMethodTypeId: (int)IRSMethodType.IrsGrossMethod,
            debit: debit,
            credit: credit);
    }
}

namespace RentAll.Test;

using RentAll.Domain.Models;

public class JournalEntryRecapScenarioTests
{
    private static readonly DateOnly MayStart = new(2026, 5, 1);
    private static readonly DateOnly MayEnd = new(2026, 5, 31);
    private static readonly DateOnly JuneStart = new(2026, 6, 1);
    private static readonly DateOnly JuneEnd = new(2026, 6, 30);
    private static readonly DateOnly MayJuneStart = MayStart;
    private static readonly DateOnly MayJuneEnd = JuneEnd;

    [Fact]
    public async Task LatePayment_JuneOnly_RollsPaymentToInvoiceWithOwnerRent()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetRecapReportAsync(JuneStart, JuneEnd);
        var rows = report.Rows.Where(row =>
            string.Equals(row.Source, OwnerReportScenarioFixtures.Invoice001, StringComparison.OrdinalIgnoreCase)).ToList();

        var rolledUpRow = Assert.Single(rows);
        Assert.Equal("05.26", rolledUpRow.AccountingPeriod);
        Assert.Equal(OwnerReportScenarioFixtures.TenantPayment001, rolledUpRow.PaymentValue);
        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent001, rolledUpRow.OwnerRentValue);
        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent001, rolledUpRow.OwnerRentActualValue);
        Assert.Equal(0m, rolledUpRow.OwnerPaymentValue);
    }

    [Fact]
    public async Task InvoicedOnly_OwnPayIsZeroWithoutOwnerPaymentJe()
    {
        var lines = new List<JournalEntryRecapLine>
        {
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                100m,
                OwnerReportScenarioFixtures.Invoice001,
                MayStart,
                MayStart,
                $"{OwnerReportScenarioFixtures.Invoice001}: Owner: Expected: Rent"),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                1000m,
                OwnerReportScenarioFixtures.Invoice001,
                MayStart,
                MayStart)
        };
        var context = ReportManagerTestSupport.CreateContext(lines);

        var report = await context.GetRecapReportAsync(MayStart, MayEnd);
        var row = Assert.Single(report.Rows);

        Assert.Equal(100m, row.OwnerRentValue);
        Assert.Equal(0m, row.OwnerRentActualValue);
        Assert.Equal(0m, row.OwnerPaymentValue);
        Assert.Equal(100m, row.UnPaidValue);
    }

    [Fact]
    public async Task LatePayment_MayAndJune_RollsEachInvoiceSeparately()
    {
        var context = ReportManagerTestSupport.CreateContext(OwnerReportScenarioFixtures.BuildLatePaymentScenarioLines());

        var report = await context.GetRecapReportAsync(MayJuneStart, MayJuneEnd);
        var mayInvoiceRow = Assert.Single(
            report.Rows,
            row => string.Equals(row.Source, OwnerReportScenarioFixtures.Invoice001, StringComparison.OrdinalIgnoreCase));
        var juneInvoiceRow = Assert.Single(
            report.Rows,
            row => string.Equals(row.Source, OwnerReportScenarioFixtures.Invoice002, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(OwnerReportScenarioFixtures.TenantPayment001, mayInvoiceRow.PaymentValue);
        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent001, mayInvoiceRow.OwnerRentValue);
        Assert.Equal(0m, juneInvoiceRow.PaymentValue);
        Assert.Equal(OwnerReportScenarioFixtures.OwnerRent002, juneInvoiceRow.UnPaidValue);
    }
}

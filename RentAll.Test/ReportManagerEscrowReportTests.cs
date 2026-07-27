namespace RentAll.Test;

public class ReportManagerEscrowReportTests
{
    [Fact]
    public async Task GetEscrowReportAsync_IncludesEscrowOwnersAccountBalanceFromBundle()
    {
        var context = ReportManagerTestSupport.CreateContext([]);
        var endDate = new DateOnly(2026, 3, 31);

        var report = await context.GetEscrowReportAsync(endDate, cushion: 25m);

        Assert.Equal(500m, report.EscrowBankBalance);
        Assert.Equal("1050 Escrow Owners", report.EscrowBankAccountLabel);
        Assert.Single(report.EscrowOfficeBalances);
        Assert.Equal(500m, report.EscrowOfficeBalances[0].Balance);
        Assert.Equal(475m, report.Transfer);
    }

    [Fact]
    public async Task GetEscrowReportAsync_NormalizesNegativeEscrowOwnersBalanceToPositive()
    {
        var context = ReportManagerTestSupport.CreateContext([]);
        context.SetEscrowOwnersBalance(-500m);
        var endDate = new DateOnly(2026, 3, 31);

        var report = await context.GetEscrowReportAsync(endDate, cushion: 25m);

        Assert.Equal(500m, report.EscrowBankBalance);
        Assert.Equal(25m, report.Cushion);
        Assert.Equal(475m, report.Transfer);
    }

    [Fact]
    public async Task GetEscrowReportAsync_CalculatesTransferAsOwnerEscrowMinusE2TotalAndCushion()
    {
        var context = ReportManagerTestSupport.CreateContext([]);
        context.SetEscrowPropertyE2Totals(apBalance: 1000m);
        var endDate = new DateOnly(2026, 3, 31);

        var report = await context.GetEscrowReportAsync(endDate, cushion: 25m);

        Assert.Equal(500m, report.EscrowBankBalance);
        Assert.Equal(1000m, report.Totals.E2);
        Assert.Equal(-525m, report.Transfer);
    }

    [Fact]
    public async Task GetEscrowReportAsync_PrepaidsUseOwnerShareFromBundlePrepaidDetails()
    {
        const string invoice = "R-001070-001";
        var decPeriod = new DateOnly(2025, 12, 1);
        var janPeriod = new DateOnly(2026, 1, 1);
        var decPaymentDate = new DateOnly(2025, 12, 15);
        var janApplyDate = new DateOnly(2026, 1, 1);
        const decimal janOwnerRent = 1669.50m;

        var context = ReportManagerTestSupport.CreateContext(
        [
            ReportManagerTestSupport.RecapLine("ExpectedIncome", 3585m, invoice, decPeriod, decPeriod),
            ReportManagerTestSupport.RecapLine("OwnerRent", 185.50m, invoice, decPeriod, decPeriod),
            ReportManagerTestSupport.RecapLine("OwnerRentActual", 185.50m, invoice, decPeriod, decPeriod),
            ReportManagerTestSupport.RecapLine("Payment", 5950m, invoice, decPeriod, decPaymentDate),
            ReportManagerTestSupport.RecapLine("PrePayment", 2385m, invoice, decPeriod, decPaymentDate, $"Prepayment: {invoice}"),
            ReportManagerTestSupport.RecapLine("ExpectedIncome", 2385m, invoice, janPeriod, janPeriod),
            ReportManagerTestSupport.RecapLine("OwnerRent", janOwnerRent, invoice, janPeriod, janPeriod),
            ReportManagerTestSupport.RecapLine("OwnerRentActual", janOwnerRent, invoice, janPeriod, janApplyDate),
            ReportManagerTestSupport.RecapLine("PrePayment", -2385m, invoice, janPeriod, janApplyDate, $"Prepayment: {invoice}")
        ]);
        context.SetEscrowPrepaidDetails(new RentAll.Domain.Models.EscrowPrepaidPropertyBalance
        {
            OfficeId = ReportManagerTestSupport.OfficeId,
            PropertyId = ReportManagerTestSupport.PropertyId,
            Prepaids = janOwnerRent
        });

        var recap = await context.GetRecapReportAsync(decPeriod, new DateOnly(2026, 1, 31));
        var ownerUnrecTotal = recap.Rows.Sum(row => row.OwnerUnrecValue);

        var report = await context.GetEscrowReportAsync(new DateOnly(2026, 1, 31));

        Assert.Equal(janOwnerRent, ownerUnrecTotal);
        Assert.Single(report.Rows);
        Assert.Equal(janOwnerRent, report.Rows[0].Prepaids);
        Assert.Equal(janOwnerRent, report.Totals.Prepaids);
    }

    [Fact]
    public async Task GetEscrowReportAsync_NotCollectedUsesOwnerRentMinusOwnerRentActual()
    {
        const decimal decOwnerRent = 185.50m;
        const decimal decOwnerAct = 100m;

        var context = ReportManagerTestSupport.CreateContext([]);
        context.SetEscrowPropertyE2Totals(apBalance: 0m);
        context.SetEscrowNotCollectedByProperty(
            ReportManagerTestSupport.OfficeId,
            ReportManagerTestSupport.PropertyId,
            decOwnerRent,
            decOwnerAct);

        var report = await context.GetEscrowReportAsync(new DateOnly(2026, 1, 31));

        Assert.Single(report.Rows);
        Assert.Equal(85.50m, report.Rows[0].NotCollected);
        Assert.Equal(85.50m, report.Totals.NotCollected);
    }
}

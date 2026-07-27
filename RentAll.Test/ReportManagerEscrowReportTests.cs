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
}

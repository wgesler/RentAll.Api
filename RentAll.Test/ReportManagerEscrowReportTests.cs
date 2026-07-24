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
        Assert.Equal(-25m, report.Transfer);
    }
}

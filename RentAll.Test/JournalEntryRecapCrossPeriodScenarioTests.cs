using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class JournalEntryRecapCrossPeriodScenarioTests
{
    private static readonly DateOnly MarchPeriod = new(2026, 3, 1);
    private static readonly DateOnly JunePeriod = new(2026, 6, 1);
    private static readonly DateOnly JulyPeriod = new(2026, 7, 1);
    private static readonly DateOnly MarchPaymentDate = new(2026, 3, 1);
    private static readonly DateOnly JunePaymentDate = new(2026, 6, 1);
    private static readonly DateOnly JulyPaymentDate = new(2026, 7, 1);

    private const string Invoice001 = "R-001053-001";
    private const string Invoice002 = "R-001053-002";
    private const string Reservation = "R-001053";

    [Fact]
    public async Task CrossPeriodInvoiceAndPrepayApply_SplitsRowsByInvoiceAndPeriod()
    {
        var context = ReportManagerTestSupport.CreateContext(BuildBar505ScenarioLines());

        var report = await context.GetRecapReportAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 7, 31));
        var rows = report.Rows
            .Where(row => string.Equals(row.ReservationCode, Reservation, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Equal(4, rows.Count);

        var marchPrepayRow = Assert.Single(rows, row => row.AccountingPeriod == "03.26");
        Assert.Equal(9100m, marchPrepayRow.PaymentValue);
        Assert.Equal(9100m, marchPrepayRow.PrePaymentValue);

        var juneInvoice001Row = Assert.Single(
            rows,
            row => row.AccountingPeriod == "06.26"
                && string.Equals(row.Source, Invoice001, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(7633.33m, juneInvoice001Row.ExpectedIncomeValue);
        Assert.Equal(7633.33m, juneInvoice001Row.PaymentValue);
        Assert.Equal(2823.33m, juneInvoice001Row.OwnerRentValue);

        var juneInvoice002Row = Assert.Single(
            rows,
            row => row.AccountingPeriod == "06.26"
                && string.Equals(row.Source, Invoice002, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(183.33m, juneInvoice002Row.ExpectedIncomeValue);
        Assert.Equal(183.33m, juneInvoice002Row.PaymentValue);
        Assert.Equal(128.33m, juneInvoice002Row.OwnerRentValue);

        var julyInvoice001Row = Assert.Single(
            rows,
            row => row.AccountingPeriod == "07.26"
                && string.Equals(row.Source, Invoice001, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1466.67m, julyInvoice001Row.ExpectedIncomeValue);
        Assert.Equal(1466.67m, julyInvoice001Row.PaymentValue);
        Assert.Equal(1026.67m, julyInvoice001Row.OwnerRentValue);
    }

    private static IReadOnlyList<JournalEntryRecapLine> BuildBar505ScenarioLines()
    {
        const decimal juneInvoice001Expected = 7633.33m;
        const decimal juneInvoice001OwnerRent = 2823.33m;
        const decimal juneInvoice002Expected = 183.33m;
        const decimal juneInvoice002OwnerRent = 128.33m;
        const decimal julyInvoice001Expected = 1466.67m;
        const decimal julyInvoice001OwnerRent = 1026.67m;

        return
        [
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                9100m,
                Invoice001,
                MarchPeriod,
                MarchPaymentDate,
                $"Prepayment: {Invoice001}",
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                juneInvoice001Expected,
                Invoice001,
                JunePeriod,
                JunePeriod),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                juneInvoice001OwnerRent,
                Invoice001,
                JunePeriod,
                JunePeriod,
                "OWNER: Rent",
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "RentPlus4000",
                4033.33m,
                Invoice001,
                JunePeriod,
                JunePeriod,
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "SecurityDeposit",
                3000m,
                Invoice001,
                JunePeriod,
                JunePeriod,
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "Fee",
                600m,
                Invoice001,
                JunePeriod,
                JunePeriod,
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                -7633.33m,
                Invoice001,
                JunePeriod,
                JunePeriod,
                $"Prepayment: {Invoice001}",
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                juneInvoice002Expected,
                Invoice002,
                JunePeriod,
                JunePeriod),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                juneInvoice002OwnerRent,
                Invoice002,
                JunePeriod,
                JunePeriod,
                "OWNER: Rent",
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "RentPlus4000",
                183.33m,
                Invoice002,
                JunePeriod,
                JunePeriod,
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                -183.33m,
                Invoice002,
                JunePeriod,
                JunePeriod,
                $"Prepayment: {Invoice002}",
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                julyInvoice001Expected,
                Invoice001,
                JulyPeriod,
                JulyPeriod),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                julyInvoice001OwnerRent,
                Invoice001,
                JulyPeriod,
                JulyPeriod,
                "OWNER: Rent",
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "RentPlus4000",
                1466.67m,
                Invoice001,
                JulyPeriod,
                JulyPeriod,
                reservationCode: Reservation),
            ReportManagerTestSupport.RecapLine(
                "Payment",
                1466.67m,
                Invoice001,
                JulyPeriod,
                JulyPaymentDate,
                $"Payment: {Invoice001}",
                sourceTypeId: (int)SourceType.InvoicePayment,
                reservationCode: Reservation)
        ];
    }
}

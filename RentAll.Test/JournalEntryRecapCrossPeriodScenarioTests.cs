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
        Assert.Equal(0m, marchPrepayRow.PaymentValue);
        Assert.Equal(9100m, marchPrepayRow.PrePaymentValue);

        var juneInvoice001Row = Assert.Single(
            rows,
            row => row.AccountingPeriod == "06.26"
                && string.Equals(row.Source, Invoice001, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(7633.33m, juneInvoice001Row.ExpectedIncomeValue);
        Assert.Equal(0m, juneInvoice001Row.PaymentValue);
        Assert.Equal(2823.33m, juneInvoice001Row.OwnerRentValue);

        var juneInvoice002Row = Assert.Single(
            rows,
            row => row.AccountingPeriod == "06.26"
                && string.Equals(row.Source, Invoice002, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(183.33m, juneInvoice002Row.ExpectedIncomeValue);
        Assert.Equal(0m, juneInvoice002Row.PaymentValue);
        Assert.Equal(128.33m, juneInvoice002Row.OwnerRentValue);

        var julyInvoice001Row = Assert.Single(
            rows,
            row => row.AccountingPeriod == "07.26"
                && string.Equals(row.Source, Invoice001, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1466.67m, julyInvoice001Row.ExpectedIncomeValue);
        Assert.Equal(1466.67m, julyInvoice001Row.PaymentValue);
        Assert.Equal(1026.67m, julyInvoice001Row.OwnerRentValue);
    }

    [Fact]
    public async Task CrossPeriodPrepayApply_OwnerUnpaidOnReceivePeriod_OwnActOnApplyPeriod()
    {
        const string Invoice = "R-001070-001";
        const string ReservationCode = "R-001070";
        var decPeriod = new DateOnly(2025, 12, 1);
        var janPeriod = new DateOnly(2026, 1, 1);
        var decPaymentDate = new DateOnly(2025, 12, 15);
        var janApplyDate = new DateOnly(2026, 1, 1);

        const decimal decExpected = 3585m;
        const decimal decOwnerRent = 185.50m;
        const decimal decOwnerAct = 185.50m;
        const decimal janExpected = 2385m;
        const decimal janOwnerRent = 1669.50m;
        const decimal janOwnerAct = 1669.50m;
        const decimal prepayAmount = 2385m;
        const decimal cashPayment = 5950m;

        var context = ReportManagerTestSupport.CreateContext(
        [
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                decExpected,
                Invoice,
                decPeriod,
                decPeriod,
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                decOwnerRent,
                Invoice,
                decPeriod,
                decPeriod,
                $"{Invoice}: Owner: Expected: Rent",
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                decOwnerAct,
                Invoice,
                decPeriod,
                decPeriod,
                $"{Invoice}: Owner: Actual: Rent",
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "Payment",
                cashPayment,
                Invoice,
                decPeriod,
                decPaymentDate,
                $"Payment: {Invoice}",
                sourceTypeId: (int)SourceType.InvoicePayment,
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                prepayAmount,
                Invoice,
                decPeriod,
                decPaymentDate,
                $"Prepayment: {Invoice}",
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                janExpected,
                Invoice,
                janPeriod,
                janPeriod,
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                janOwnerRent,
                Invoice,
                janPeriod,
                janPeriod,
                $"{Invoice}: Owner: Expected: Rent",
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                janOwnerAct,
                Invoice,
                janPeriod,
                janApplyDate,
                $"{Invoice}: Owner: Actual: Rent",
                reservationCode: ReservationCode),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                -prepayAmount,
                Invoice,
                janPeriod,
                janApplyDate,
                $"Prepayment: {Invoice}",
                reservationCode: ReservationCode)
        ]);

        var report = await context.GetRecapReportAsync(decPeriod, new DateOnly(2026, 1, 31));
        var rows = report.Rows
            .Where(row => string.Equals(row.Source, Invoice, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Equal(2, rows.Count);

        var decRow = Assert.Single(rows, row => row.AccountingPeriod == "12.25");
        Assert.Equal(decExpected, decRow.ExpectedIncomeValue);
        Assert.Equal(cashPayment, decRow.PaymentValue);
        Assert.Equal(prepayAmount, decRow.PrePaymentValue);
        Assert.Equal(decOwnerRent, decRow.OwnerRentValue);
        Assert.Equal(decOwnerAct, decRow.OwnerRentActualValue);
        Assert.Equal(janOwnerRent, decRow.OwnerUnrecValue);
        Assert.Equal(0m, decRow.UnPaidValue);

        var janRow = Assert.Single(rows, row => row.AccountingPeriod == "01.26");
        Assert.Equal(janExpected, janRow.ExpectedIncomeValue);
        Assert.Equal(0m, janRow.PaymentValue);
        Assert.Equal(-prepayAmount, janRow.PrePaymentValue);
        Assert.Equal(janOwnerRent, janRow.OwnerRentValue);
        Assert.Equal(janOwnerAct, janRow.OwnerRentActualValue);
        Assert.Equal(0m, janRow.OwnerUnrecValue);
        Assert.Equal(0m, janRow.UnPaidValue);
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
                "R-001053-001: Owner: Expected: Rent",
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
                $"{Invoice002}: Owner: Expected: Rent",
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
                "R-001053-001: Owner: Expected: Rent",
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

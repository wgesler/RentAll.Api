using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

internal static class OwnerReportScenarioFixtures
{
    internal const string Invoice001 = "R-000177-001";
    internal const string Invoice002 = "R-000177-002";
    internal const string CrossPeriodInvoice = "R-00087-001";
    internal const string CrossPeriodReservation = "R-00087";
    internal const string Buck805Invoice001 = "R-000153-001";
    internal const string Buck805Invoice002 = "R-000153-002";
    internal const string Buck805Invoice003 = "R-000079-001";
    internal const string Buck805Reservation153 = "R-000153";
    internal const string Buck805Reservation079 = "R-000079";

    internal const decimal Buck805OwnerRent001 = 980m;
    internal const decimal Buck805OwnerRent002 = 490m;
    internal const decimal Buck805OwnerRent003 = 924m;

    internal const decimal OwnerRent001 = 49.70m;
    internal const decimal ExpectedIncome001 = 2130m;
    internal const decimal TenantPayment001 = 2130m;
    internal const decimal OwnerRent002 = 1491m;
    internal const string OwnerRent001ExpectedMemo = "R-000177-001: Owner: Expected: Rent";
    internal const string OwnerRent002ExpectedMemo = "R-000177-002: Owner: Expected: Rent";

    internal static readonly DateOnly MayPeriod = new(2026, 5, 1);
    internal static readonly DateOnly JunePeriod = new(2026, 6, 1);
    internal static readonly DateOnly JulyPeriod = new(2026, 7, 1);
    internal static readonly DateOnly MayPaymentDate = new(2026, 5, 15);
    internal static readonly DateOnly JunePaymentDate = new(2026, 6, 10);
    internal static readonly DateOnly AprilPaymentDate = new(2026, 4, 28);

    internal static readonly Guid LatePaymentLedgerLineId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    internal static readonly Guid PrepaymentLedgerLineId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    internal static IReadOnlyList<JournalEntryRecapLine> BuildLatePaymentScenarioLines()
    {
        return
        [
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                OwnerRent001,
                Invoice001,
                MayPeriod,
                MayPeriod,
                OwnerRent001ExpectedMemo),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                ExpectedIncome001,
                Invoice001,
                MayPeriod,
                MayPeriod),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                OwnerRent002,
                Invoice002,
                JunePeriod,
                JunePeriod,
                OwnerRent002ExpectedMemo),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                44730m,
                Invoice002,
                JunePeriod,
                JunePeriod),
            ReportManagerTestSupport.RecapLine(
                "Payment",
                TenantPayment001,
                Invoice001,
                JunePeriod,
                JunePaymentDate,
                $"R-000177-001: Payment: {Invoice001}",
                LatePaymentLedgerLineId,
                (int)SourceType.InvoicePayment),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                OwnerRent001,
                Invoice001,
                JunePeriod,
                JunePaymentDate,
                "R-000177-001: Owner: Actual: Rent",
                LatePaymentLedgerLineId,
                (int)SourceType.InvoicePayment)
        ];
    }

    internal static IReadOnlyList<JournalEntryRecapLine> BuildPrepaymentScenarioLines()
    {
        const decimal firstSliceOwnerRent = 49.70m;
        const decimal firstSliceExpectedIncome = 2632m;
        const decimal prepaymentAmount = 2632m;

        return
        [
            ReportManagerTestSupport.RecapLine(
                "Payment",
                prepaymentAmount,
                CrossPeriodInvoice,
                MayPeriod,
                MayPaymentDate,
                $"Payment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                prepaymentAmount,
                CrossPeriodInvoice,
                MayPeriod,
                MayPaymentDate,
                $"Prepayment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                firstSliceOwnerRent,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                $"{CrossPeriodInvoice}: Owner: Expected: Rent",
                reservationCode: CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                firstSliceExpectedIncome,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                reservationCode: CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                -prepaymentAmount,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                $"R-00087-001: Prepayment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.Invoice,
                CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                firstSliceOwnerRent,
                CrossPeriodInvoice,
                JunePeriod,
                MayPaymentDate,
                $"{CrossPeriodInvoice}: Owner: Actual: Rent",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                CrossPeriodReservation)
        ];
    }

    internal static IReadOnlyList<JournalEntryRecapLine> BuildBuck805JuneScenarioLines()
    {
        const decimal prepaymentAmount = 980m;

        return
        [
            ReportManagerTestSupport.RecapLine(
                "Payment",
                prepaymentAmount,
                Buck805Invoice001,
                MayPeriod,
                AprilPaymentDate,
                $"Payment: {Buck805Invoice001}",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                prepaymentAmount,
                Buck805Invoice001,
                MayPeriod,
                AprilPaymentDate,
                $"Prepayment: {Buck805Invoice001}",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                Buck805OwnerRent001,
                Buck805Invoice001,
                JunePeriod,
                JunePeriod,
                $"{Buck805Invoice001}: Owner: Expected: Rent",
                reservationCode: Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                4200m,
                Buck805Invoice001,
                JunePeriod,
                JunePeriod,
                reservationCode: Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                Buck805OwnerRent001,
                Buck805Invoice001,
                JunePeriod,
                AprilPaymentDate,
                $"{Buck805Invoice001}: Owner: Actual: Rent",
                PrepaymentLedgerLineId,
                (int)SourceType.Invoice,
                Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                Buck805OwnerRent002,
                Buck805Invoice002,
                JunePeriod,
                JunePeriod,
                $"{Buck805Invoice002}: Owner: Expected: Rent",
                reservationCode: Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                2100m,
                Buck805Invoice002,
                JunePeriod,
                JunePeriod,
                reservationCode: Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                Buck805OwnerRent002,
                Buck805Invoice002,
                JunePeriod,
                JunePaymentDate,
                $"{Buck805Invoice002}: Owner: Actual: Rent",
                LatePaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                Buck805Reservation153),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                Buck805OwnerRent003,
                Buck805Invoice003,
                JunePeriod,
                JunePeriod,
                $"{Buck805Invoice003}: Owner: Expected: Rent",
                reservationCode: Buck805Reservation079),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                3960m,
                Buck805Invoice003,
                JunePeriod,
                JunePeriod,
                reservationCode: Buck805Reservation079),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                Buck805OwnerRent003,
                Buck805Invoice003,
                JunePeriod,
                JunePaymentDate,
                $"{Buck805Invoice003}: Owner: Actual: Rent",
                LatePaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                Buck805Reservation079)
        ];
    }

    internal static IReadOnlyList<JournalEntryRecapLine> BuildCrossPeriodPrepaymentScenarioLines()
    {
        const decimal juneSliceOwnerRent = 60m;
        const decimal julySliceOwnerRent = 40m;
        const decimal juneSliceExpectedIncome = 1588m;
        const decimal julySliceExpectedIncome = 1058m;
        const decimal juneSliceApplyAmount = 1588m;
        const decimal julyPrepaidRemainder = 1058m;
        const decimal totalPrepayment = juneSliceApplyAmount + julyPrepaidRemainder;

        return
        [
            ReportManagerTestSupport.RecapLine(
                "Payment",
                totalPrepayment,
                CrossPeriodInvoice,
                MayPeriod,
                MayPaymentDate,
                $"Payment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                totalPrepayment,
                CrossPeriodInvoice,
                MayPeriod,
                MayPaymentDate,
                $"Prepayment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                juneSliceOwnerRent,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                $"{CrossPeriodInvoice}: Owner: Expected: Rent June slice",
                reservationCode: CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                juneSliceExpectedIncome,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                reservationCode: CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "OwnerRent",
                julySliceOwnerRent,
                CrossPeriodInvoice,
                JulyPeriod,
                JulyPeriod,
                $"{CrossPeriodInvoice}: Owner: Expected: Rent July slice",
                reservationCode: CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "ExpectedIncome",
                julySliceExpectedIncome,
                CrossPeriodInvoice,
                JulyPeriod,
                JulyPeriod,
                reservationCode: CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "PrePayment",
                -juneSliceApplyAmount,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                $"R-00087-001: Prepayment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.Invoice,
                CrossPeriodReservation),
            ReportManagerTestSupport.RecapLine(
                "OwnerRentActual",
                juneSliceOwnerRent,
                CrossPeriodInvoice,
                JunePeriod,
                JunePeriod,
                $"{CrossPeriodInvoice}: Owner: Actual: Rent June slice",
                PrepaymentLedgerLineId,
                (int)SourceType.InvoicePayment,
                CrossPeriodReservation)
        ];
    }
}

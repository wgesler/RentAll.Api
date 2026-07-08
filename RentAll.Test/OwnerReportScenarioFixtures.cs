using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

internal static class OwnerReportScenarioFixtures
{
    internal const string Invoice001 = "R-000177-001";
    internal const string Invoice002 = "R-000177-002";
    internal const string CrossPeriodInvoice = "R-00087-001";
    internal const string CrossPeriodReservation = "R-00087";

    internal const decimal OwnerRent001 = 49.70m;
    internal const decimal ExpectedIncome001 = 2130m;
    internal const decimal TenantPayment001 = 2130m;
    internal const decimal OwnerRent002 = 1491m;

    internal static readonly DateOnly MayPeriod = new(2026, 5, 1);
    internal static readonly DateOnly JunePeriod = new(2026, 6, 1);
    internal static readonly DateOnly JulyPeriod = new(2026, 7, 1);
    internal static readonly DateOnly MayPaymentDate = new(2026, 5, 15);
    internal static readonly DateOnly JunePaymentDate = new(2026, 6, 10);

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
                "OWNER: Rent"),
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
                "OWNER: Rent"),
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
                $"Payment: {Invoice001}",
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
                "OWNER: Rent",
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
                $"Prepayment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.Invoice,
                CrossPeriodReservation)
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
                "OWNER: Rent June slice",
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
                "OWNER: Rent July slice",
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
                $"Prepayment: {CrossPeriodInvoice}",
                PrepaymentLedgerLineId,
                (int)SourceType.Invoice,
                CrossPeriodReservation)
        ];
    }
}

using RentAll.Domain.Enums;
using RentAll.Domain.Helpers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class RentalFeeLineParserTests
{
    [Fact]
    public void CalculateDaysBilledForBillingPeriod_SplitsCrossMonthRentalLine()
    {
        var reservation = CreateReservation(BillingType.Monthly, new DateOnly(2026, 7, 20));
        reservation.ArrivalDate = new DateOnly(2026, 6, 21);
        var reservationId = reservation.ReservationId;
        var invoiceStart = new DateOnly(2026, 6, 1);
        var invoices = new[]
        {
            BuildInvoice(reservationId, new DateOnly(2026, 6, 1), "2026-06-01 - 2026-07-20", [
                new LedgerLine { Description = "Rental Fee (06/21-07/20)", Amount = 3000m }
            ])
        };

        var juneDays = RentalFeeLineParser.CalculateDaysBilledForBillingPeriod(
            invoices,
            reservation,
            invoiceStart,
            new DateOnly(2026, 6, 21),
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 7, 20));

        var julyDays = RentalFeeLineParser.CalculateDaysBilledForBillingPeriod(
            invoices,
            reservation,
            invoiceStart,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 20),
            new DateOnly(2026, 7, 20));

        Assert.Equal(10, juneDays);
        Assert.Equal(20, julyDays);
    }

    [Fact]
    public void TryParseRentalFeePeriod_IgnoresSuffixAfterDateRange()
    {
        Assert.True(
            InvoiceBillingDays.TryParseRentalFeePeriod(
                "Rental Fee (06/01-08/27) - $148/D",
                2026,
                out var start,
                out var end));
        Assert.Equal(new DateOnly(2026, 6, 1), start);
        Assert.Equal(new DateOnly(2026, 8, 27), end);
    }

    [Fact]
    public void CalculateDaysBilledForBillingPeriod_FirstThirtyDaysOnJulyInvoiceCoversAugustSlice()
    {
        var reservation = CreateReservation(BillingType.Monthly, new DateOnly(2026, 8, 9));
        reservation.ArrivalDate = new DateOnly(2026, 7, 11);
        var invoices = new[]
        {
            BuildInvoice(reservation.ReservationId, new DateOnly(2026, 7, 1), "07/11/2026 - 08/09/2026", [
                new LedgerLine { Description = "Rental Fee (07/11-08/09)", Amount = 5850m },
                new LedgerLine { Description = "Check 07/11-07/31", Amount = 4095m },
                new LedgerLine { Description = "Check 08/01-08/09", Amount = 1755m }
            ])
        };

        var daysBilled = RentalFeeLineParser.CalculateDaysBilledForBillingPeriod(
            invoices,
            reservation,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 9),
            new DateOnly(2026, 8, 9));

        var gaps = RentalFeeLineParser.FindUncoveredBillingRangesForPeriod(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 9),
            invoices,
            new DateOnly(2026, 8, 1));

        Assert.Equal(9, daysBilled);
        Assert.Empty(gaps);
    }

    [Fact]
    public void TryParseRentalFeePeriod_AcceptsEnDash()
    {
        Assert.True(InvoiceBillingDays.TryParseRentalFeePeriod(
            "Rental Fee (07/11\u201308/09)",
            2026,
            out var start,
            out var end));
        Assert.Equal(new DateOnly(2026, 7, 11), start);
        Assert.Equal(new DateOnly(2026, 8, 9), end);
    }

    [Fact]
    public void CalculateDaysBilledFromInvoices_CountsRentalLineWithRateSuffixAndNegativeAmount()
    {
        var reservation = CreateReservation(BillingType.Monthly);
        var reservationId = reservation.ReservationId;
        var invoices = new[]
        {
            BuildInvoice(reservationId, new DateOnly(2026, 8, 1), "2026-08-01 - 2026-08-31", [
                new LedgerLine { Description = "Rental Fee (06/01-08/27) - $148/D", Amount = -148m }
            ])
        };

        var daysBilled = RentalFeeLineParser.CalculateDaysBilledForBillingPeriod(
            invoices,
            reservation,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 27),
            new DateOnly(2026, 8, 27));

        Assert.Equal(27, daysBilled);
    }

    private static Reservation CreateReservation(BillingType billingType, DateOnly? departure = null)
        => new()
        {
            ReservationId = Guid.NewGuid(),
            ArrivalDate = new DateOnly(2026, 1, 1),
            DepartureDate = departure ?? new DateOnly(2026, 12, 31),
            BillingType = billingType
        };

    private static Invoice BuildInvoice(
        Guid reservationId,
        DateOnly accountingPeriod,
        string? invoicePeriod,
        List<LedgerLine> ledgerLines)
        => new()
        {
            ReservationId = reservationId,
            AccountingPeriod = accountingPeriod,
            InvoiceDate = accountingPeriod,
            InvoicePeriod = invoicePeriod,
            IsActive = true,
            LedgerLines = ledgerLines
        };
}

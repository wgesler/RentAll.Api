using RentAll.Domain;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class RentalFeeLineParserTests
{
    [Fact]
    public void CalculateDaysBilledFromInvoices_SumsGetChargesDaysPerRentalLine()
    {
        var reservation = CreateReservation(BillingType.Monthly);
        var reservationId = reservation.ReservationId;
        var invoices = new[]
        {
            BuildInvoice(reservationId, new DateOnly(2026, 9, 1), "2026-09-01 - 2026-09-30", [
                new LedgerLine { Description = "Rental Fee (09/01-09/15)", Amount = 1500m }
            ]),
            BuildInvoice(reservationId, new DateOnly(2026, 9, 1), "2026-09-01 - 2026-09-30", [
                new LedgerLine { Description = "Rental Fee (09/10-09/22)", Amount = 1300m }
            ])
        };

        var daysBilled = RentalFeeLineParser.CalculateDaysBilledFromInvoices(
            invoices,
            reservation,
            new DateOnly(2026, 6, 1));

        Assert.Equal(22, daysBilled);
    }

    [Fact]
    public void CollectRentalFeeLineDescriptions_ReturnsDistinctSortedLines()
    {
        var reservationId = Guid.NewGuid();
        var invoices = new[]
        {
            BuildInvoice(reservationId, new DateOnly(2026, 9, 1), null, [
                new LedgerLine { Description = "Rental Fee (09/16-09/22)", Amount = 700m },
                new LedgerLine { Description = "Rental Fee (09/01-09/15)", Amount = 1500m },
                new LedgerLine { Description = "Pet Fee", Amount = 100m }
            ])
        };

        var lines = RentalFeeLineParser.CollectRentalFeeLineDescriptions(invoices, new DateOnly(2026, 6, 1));

        Assert.Equal(2, lines.Count);
        Assert.Equal("Rental Fee (09/01-09/15)", lines[0]);
        Assert.Equal("Rental Fee (09/16-09/22)", lines[1]);
    }

    [Fact]
    public void CalculateDaysBilledFromInvoices_UsesInvoiceStartForInvoicesAndClipsRentalRanges()
    {
        var reservation = CreateReservation(BillingType.Monthly);
        var reservationId = reservation.ReservationId;
        var invoiceStart = new DateOnly(2026, 1, 1);
        var invoices = new[]
        {
            BuildInvoice(reservationId, new DateOnly(2025, 12, 1), "2025-12-01 - 2025-12-31", [
                new LedgerLine { Description = "Rental Fee (12/01-12/31)", Amount = 1500m }
            ]),
            BuildInvoice(reservationId, new DateOnly(2026, 1, 1), "2026-01-01 - 2026-01-31", [
                new LedgerLine { Description = "Rental Fee (01/01-01/31)", Amount = 1500m }
            ])
        };

        var daysBilled = RentalFeeLineParser.CalculateDaysBilledFromInvoices(invoices, reservation, invoiceStart);

        Assert.Equal(31, daysBilled);
    }

    [Theory]
    [InlineData(BillingType.Daily, 23)]
    [InlineData(BillingType.Monthly, 23)]
    [InlineData(BillingType.Nightly, 22)]
    public void CalculateDaysBilledFromInvoices_MatchesGetChargesDayCount(BillingType billingType, int expectedDays)
    {
        var reservation = CreateReservation(billingType, new DateOnly(2026, 9, 23));
        var reservationId = reservation.ReservationId;
        var invoices = new[]
        {
            BuildInvoice(
                reservationId,
                new DateOnly(2026, 9, 1),
                "2026-09-01 - 2026-09-23",
                [
                    new LedgerLine
                    {
                        Description = billingType == BillingType.Nightly
                            ? "Rental Fee (09/01-09/22)"
                            : "Rental Fee (09/01-09/23)",
                        Amount = 1500m
                    }
                ])
        };

        var daysBilled = RentalFeeLineParser.CalculateDaysBilledFromInvoices(
            invoices,
            reservation,
            new DateOnly(2026, 6, 1),
            billingStayEndDate: new DateOnly(2026, 9, 23));

        Assert.Equal(expectedDays, daysBilled);
        Assert.Equal(
            InvoiceBillingDays.CalculateNumberOfDays(
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 23),
                billingType,
                isDepartureMonthYear: true,
                isLastDayOfMonth: false),
            daysBilled);
    }

    [Fact]
    public void CalculateDaysBilledFromInvoices_IncludesJuneInvoiceWhenInvoiceStartIsJuneFirst()
    {
        var reservation = CreateReservation(BillingType.Monthly);
        var reservationId = reservation.ReservationId;
        var invoiceStart = new DateOnly(2026, 6, 1);
        var invoices = new[]
        {
            BuildInvoice(reservationId, new DateOnly(2026, 5, 1), "2026-05-01 - 2026-05-31", [
                new LedgerLine { Description = "Rental Fee (05/01-05/31)", Amount = 1500m }
            ]),
            BuildInvoice(reservationId, new DateOnly(2026, 6, 1), "2026-06-01 - 2026-06-30", [
                new LedgerLine { Description = "Rental Fee (06/01-06/30)", Amount = 1500m }
            ])
        };

        var daysBilled = RentalFeeLineParser.CalculateDaysBilledFromInvoices(invoices, reservation, invoiceStart);

        Assert.Equal(30, daysBilled);
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

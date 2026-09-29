using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class BilledMatchupRowTests
{
    [Fact]
    public void GetLedgerLines_FullMonth_ReturnsRentalFeeLine()
    {
        var reservation = CreateLongStayReservation(new DateOnly(2026, 12, 31));
        var manager = CreateManager();

        var lines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            77);

        Assert.Contains(lines, line => (line.Description ?? string.Empty).StartsWith("Rental Fee", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildBilledMatchupMonthlyRows_EmitsOneRowPerBillableMonthFromInvoiceStart()
    {
        var reservation = CreateLongStayReservation(new DateOnly(2026, 9, 15));
        reservation.ReservationId = Guid.NewGuid();
        reservation.ReservationCode = "R-000000099";
        reservation.OfficeId = 5;
        var asOf = new DateOnly(2026, 9, 28);
        var manager = CreateManager();

        var rows = manager.BuildBilledMatchupMonthlyRows(
            reservation,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            asOf);

        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.True(row.MonthStart >= BilledMatchupInvoiceStart.InvoiceStart));
        Assert.Equal(new DateOnly(2026, 6, 1), rows[0].MonthStart);
        Assert.Equal(new DateOnly(2026, 9, 1), rows[^1].MonthStart);
        var september = Assert.Single(rows, row => row.MonthStart == new DateOnly(2026, 9, 1));
        Assert.Equal(15, september.DaysStayed);
    }

    [Fact]
    public void BuildBilledMatchupMonthlyRows_FullJuneWhenStayContinuesAfterBillingEnd()
    {
        var reservation = CreateLongStayReservation(new DateOnly(2026, 8, 27));
        reservation.ReservationId = Guid.NewGuid();
        reservation.ReservationCode = "R-BILLING-END-LAG";
        reservation.OfficeId = 5;
        reservation.BillingType = BillingType.Daily;
        reservation.ArrivalDate = new DateOnly(2026, 6, 1);
        reservation.DepartureDate = new DateOnly(2026, 8, 27);
        reservation.BillingEndDate = new DateOnly(2026, 6, 29);
        var manager = CreateManager();

        var rows = manager.BuildBilledMatchupMonthlyRows(
            reservation,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            new DateOnly(2026, 8, 27));

        var june = Assert.Single(rows, row => row.MonthStart == new DateOnly(2026, 6, 1));
        Assert.Equal(new DateOnly(2026, 6, 1), june.PeriodStart);
        Assert.Equal(new DateOnly(2026, 6, 30), june.PeriodEnd);
        Assert.Equal(30, june.DaysStayed);
    }

    [Fact]
    public void BuildBilledMatchupMonthlyRows_R000252Style_JuneJulyFullCalendarMonths()
    {
        var reservation = CreateLongStayReservation(new DateOnly(2026, 8, 9));
        reservation.ReservationId = Guid.NewGuid();
        reservation.ReservationCode = "R-000252";
        reservation.OfficeId = 5;
        reservation.BillingType = BillingType.Daily;
        reservation.ArrivalDate = new DateOnly(2026, 5, 30);
        reservation.DepartureDate = new DateOnly(2026, 8, 9);
        reservation.BillingEndDate = new DateOnly(2026, 7, 30);
        var manager = CreateManager();

        var rows = manager.BuildBilledMatchupMonthlyRows(
            reservation,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            new DateOnly(2026, 9, 29));

        var june = Assert.Single(rows, row => row.MonthStart == new DateOnly(2026, 6, 1));
        Assert.Equal(new DateOnly(2026, 6, 30), june.PeriodEnd);
        Assert.Equal(30, june.DaysStayed);

        var july = Assert.Single(rows, row => row.MonthStart == new DateOnly(2026, 7, 1));
        Assert.Equal(new DateOnly(2026, 7, 31), july.PeriodEnd);
        Assert.Equal(31, july.DaysStayed);
    }

    private static Reservation CreateLongStayReservation(DateOnly departure)
        => new()
        {
            ArrivalDate = new DateOnly(2025, 4, 18),
            DepartureDate = departure,
            BillingType = BillingType.Monthly,
            ProrateType = ProrateType.SecondMonth,
            BillingRate = 3000m,
            DepositType = DepositType.CLR,
            MaidStartDate = new DateOnly(2100, 1, 1),
            Frequency = FrequencyType.Monthly,
            ExtraFeeLines = []
        };

    private static AccountingManager CreateManager()
        => new(
            organizationRepository: null!,
            propertyRepository: null!,
            accountingRepository: null!,
            maintenanceRepository: null!,
            reservationRepository: null!,
            journalEntryRepository: null!,
            organizationManager: null!,
            contactRepository: null!,
            featureFlagService: new EnabledFeatureFlagService(),
            healthRepository: null!);

    private sealed class EnabledFeatureFlagService : IFeatureFlagService
    {
        public IReadOnlyDictionary<string, bool> GetAll()
            => new Dictionary<string, bool>();

        public bool IsEnabled(string featureName) => true;

        public Task<bool> IsEnabledAsync(string featureName, Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public void Set(string featureName, bool enabled)
        {
        }
    }
}

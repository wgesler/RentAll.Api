using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces;
using RentAll.Domain.Interfaces.Repositories;
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
    public void BuildBilledMatchupRow_DaysStayed_UsesGetChargesLogicThroughSeptember()
    {
        var reservation = CreateLongStayReservation(new DateOnly(2026, 12, 31));
        reservation.ReservationId = Guid.Parse("AC1F7A72-40C6-4B73-BD14-181AFBF16A14");
        reservation.ReservationCode = "R-000000042";
        reservation.OfficeId = 3;
        var asOf = new DateOnly(2026, 9, 28);
        var manager = CreateManager();

        var row = manager.BuildBilledMatchupRow(
            reservation,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            asOf);

        Assert.Equal(623, row.TotalNumberOfDays);
        Assert.Equal(BilledMatchupInvoiceStart.InvoiceStart, row.InvoiceStart);
        Assert.Equal(122, row.DaysSinceStart);
        Assert.Equal(120, row.DaysStayed);
        Assert.Equal(new DateOnly(2025, 4, 18), row.StartDate);
    }

    [Fact]
    public void BuildBilledMatchupRow_DaysSinceStart_ExtendsThroughMonthEnd_WhenGuestLeavesMidMonth()
    {
        var reservation = CreateLongStayReservation(new DateOnly(2026, 9, 15));
        reservation.ReservationId = Guid.NewGuid();
        reservation.ReservationCode = "R-000000099";
        reservation.OfficeId = 5;
        var asOf = new DateOnly(2026, 9, 28);
        var manager = CreateManager();

        var row = manager.BuildBilledMatchupRow(
            reservation,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            asOf);

        Assert.Equal(BilledMatchupInvoiceStart.InvoiceStart, row.InvoiceStart);
        Assert.Equal(107, row.DaysSinceStart);
        Assert.Equal(107, row.DaysStayed);
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

using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Test;

public class AccountingManagerLedgerLineTests
{
    #region Matrix Test Functions

    [Theory]
    [MemberData(nameof(LedgerLineScenarioMatrix))]
    public void GetLedgerLines_AllRequestedCombinations_ReturnExpectedRentalLine(ProrateType prorateType, BillingType billingType, DateOnly arrival, DateOnly departure, DateOnly startDate, DateOnly endDate, string expectedDescription, int expectedDays, decimal expectedAmount, int expectedCostCodeId)
    {
        Console.WriteLine(
            $"{prorateType},{billingType},{arrival:MM/dd/yyyy},{departure:MM/dd/yyyy},{startDate:MM/dd/yyyy},{endDate:MM/dd/yyyy}");
        Console.WriteLine(
            $"{expectedDescription},{expectedDays},{expectedAmount:0.##},{expectedCostCodeId}");

        var reservation = CreateReservation(
            arrival: arrival,
            departure: departure,
            prorateType: prorateType,
            billingType: billingType,
            billingRate: GetBillingRate(billingType));
        var manager = CreateManager();

        var lines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            startDate: startDate,
            endDate: endDate,
            rentalCostCodeId: 77);

        var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));

        Assert.Equal(expectedDescription, rental.Description);
        Assert.Equal(expectedAmount, rental.Amount);
    }

    public static IEnumerable<object[]> LedgerLineScenarioMatrix()
    {
        var rows = new[]
        {
            "FirstMonth,Monthly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "FirstMonth,Monthly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-04/30),29,2900,77",
            "FirstMonth,Monthly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-04/30),26,2600,77",
            "FirstMonth,Monthly,04/01/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/01-05/31),31,3000,0",
            "FirstMonth,Monthly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,77",
            "FirstMonth,Monthly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "FirstMonth,Monthly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3000,0",
            "FirstMonth,Daily,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "FirstMonth,Daily,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-04/30),29,2900,0",
            "FirstMonth,Daily,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-04/30),26,2600,0",
            "FirstMonth,Daily,04/01/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/01-05/31),31,3100,0",
            "FirstMonth,Daily,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,0",
            "FirstMonth,Daily,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "FirstMonth,Daily,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3100,0",
            "FirstMonth,Nightly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "FirstMonth,Nightly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-04/30),29,2900,0",
            "FirstMonth,Nightly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-04/30),26,2600,0",
            "FirstMonth,Nightly,04/01/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/01-05/31),31,3100,0",
            "FirstMonth,Nightly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/14),14,1400,0",
            "FirstMonth,Nightly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),29,2900,0",
            "FirstMonth,Nightly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),30,3000,0",
            "SecondMonth,Monthly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "SecondMonth,Monthly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-05/01),30,3000,0",
            "SecondMonth,Monthly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-05/04),30,3000,0",
            "SecondMonth,Monthly,04/02/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/02-05/31),30,3000,0",
            "SecondMonth,Monthly,04/05/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/31),27,2700,0",
            "SecondMonth,Monthly,04/05/2026,05/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/15),11,1100,0",
            "SecondMonth,Monthly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,77",
            "SecondMonth,Monthly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "SecondMonth,Monthly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3000,0",
            "SecondMonth,Daily,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "SecondMonth,Daily,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-05/01),30,3000,0",
            "SecondMonth,Daily,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-05/04),30,3000,0",
            "SecondMonth,Daily,04/02/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/02-05/31),30,3000,0",
            "SecondMonth,Daily,04/05/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/31),27,2700,0",
            "SecondMonth,Daily,04/05/2026,05/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/15),11,1100,0",
            "SecondMonth,Daily,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,0",
            "SecondMonth,Daily,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "SecondMonth,Daily,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3100,0",
            "SecondMonth,Nightly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "SecondMonth,Nightly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-05/01),30,3000,0",
            "SecondMonth,Nightly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-05/04),30,3000,0",
            "SecondMonth,Nightly,04/02/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/02-05/31),30,3000,0",
            "SecondMonth,Nightly,04/05/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/31),27,2700,0",
            "SecondMonth,Nightly,04/05/2026,05/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/14),10,1000,0",
            "SecondMonth,Nightly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/14),14,1400,0",
            "SecondMonth,Nightly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),29,2900,0",
            "SecondMonth,Nightly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),30,3000,0"
        };

        foreach (var row in rows)
        {
            var cols = row.Split(',');
            yield return new object[]
            {
                Enum.Parse<ProrateType>(cols[0]),
                Enum.Parse<BillingType>(cols[1]),
                DateOnly.ParseExact(cols[2], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[3], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[4], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[5], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                cols[6],
                int.Parse(cols[7], CultureInfo.InvariantCulture),
                decimal.Parse(cols[8], CultureInfo.InvariantCulture),
                int.Parse(cols[9], CultureInfo.InvariantCulture)
            };
        }
    }

    #endregion

    #region Focused Edge Cases

    [Fact]
    public void GetLedgerLines_FirstMonthProrate_ArrivalOnMonthEnd_ProrationIsSingleDay()
    {
        var reservation = CreateReservation(
            arrival: new DateOnly(2026, 4, 30),
            departure: new DateOnly(2026, 5, 31),
            prorateType: ProrateType.FirstMonth,
            billingType: BillingType.Monthly,
            billingRate: 3000m);
        var manager = CreateManager();

        var lines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            startDate: new DateOnly(2026, 4, 1),
            endDate: new DateOnly(2026, 4, 30),
            rentalCostCodeId: 77);

        var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (04/30-04/30)", rental.Description);
        Assert.Equal(100m, rental.Amount);
    }

    [Fact]
    public void GetLedgerLines_FirstMonthProrate_ArrivalOnFirstDay_BillsFullMonth()
    {
        var reservation = CreateReservation(
            arrival: new DateOnly(2026, 4, 1),
            departure: new DateOnly(2026, 5, 31),
            prorateType: ProrateType.FirstMonth,
            billingType: BillingType.Monthly,
            billingRate: 3000m);
        var manager = CreateManager();

        var lines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            startDate: new DateOnly(2026, 4, 1),
            endDate: new DateOnly(2026, 4, 30),
            rentalCostCodeId: 77);

        var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (04/01-04/30)", rental.Description);
        Assert.Equal(3000m, rental.Amount);
    }

    [Fact]
    public void GetLedgerLines_FirstMonthProrate_MidMonthArrival_ProratesToMonthEnd()
    {
        var reservation = CreateReservation(
            arrival: new DateOnly(2026, 4, 15),
            departure: new DateOnly(2026, 7, 31),
            prorateType: ProrateType.FirstMonth,
            billingType: BillingType.Monthly,
            billingRate: 3000m);
        var manager = CreateManager();

        var lines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            startDate: new DateOnly(2026, 4, 1),
            endDate: new DateOnly(2026, 4, 30),
            rentalCostCodeId: 77);

        var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (04/15-04/30)", rental.Description);
        Assert.Equal(1600m, rental.Amount);
    }

    [Fact]
    public void GetLedgerLines_FirstMonthProrate_ArrivalAndDepartureSameMonth_UsesStayRange()
    {
        var reservation = CreateReservation(
            arrival: new DateOnly(2026, 4, 10),
            departure: new DateOnly(2026, 4, 20),
            prorateType: ProrateType.FirstMonth,
            billingType: BillingType.Monthly,
            billingRate: 3000m);
        var manager = CreateManager();

        var lines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            startDate: new DateOnly(2026, 4, 1),
            endDate: new DateOnly(2026, 4, 30),
            rentalCostCodeId: 77);

        var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (04/10-04/20)", rental.Description);
        Assert.Equal(1100m, rental.Amount);
    }

    #endregion

    #region Print Helpers

    [Fact]
    public void PrintLedgerLineScenarioReport_Table()
    {
        var rows = new List<string>();
        rows.Add("| Prorate | Billing | Arrival | Departure | Start | End | Rental Description | Days | Amount | CostCode |");
        rows.Add("|---|---|---|---|---|---|---|---:|---:|---:|");

        foreach (var scenario in LedgerLineScenarioMatrix())
        {
            var prorateType = (ProrateType)scenario[0];
            var billingType = (BillingType)scenario[1];
            var arrival = (DateOnly)scenario[2];
            var departure = (DateOnly)scenario[3];
            var startDate = (DateOnly)scenario[4];
            var endDate = (DateOnly)scenario[5];
            var expectedDays = (int)scenario[7];

            var reservation = CreateReservation(
                arrival: arrival,
                departure: departure,
                prorateType: prorateType,
                billingType: billingType,
                billingRate: GetBillingRate(billingType));
            var manager = CreateManager();

            var lines = manager.GetLedgerLinesByReservationIdAsync(
                reservation,
                startDate: startDate,
                endDate: endDate,
                rentalCostCodeId: 77);
            var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));

            rows.Add(
                $"| {prorateType} | {billingType} | {arrival:MM/dd/yyyy} | {departure:MM/dd/yyyy} | {startDate:MM/dd/yyyy} | {endDate:MM/dd/yyyy} | {rental.Description} | {expectedDays} | {rental.Amount:0.##} | {rental.CostCodeId} |");
        }

        Assert.NotEmpty(rows);
        foreach (var row in rows)
            Console.WriteLine(row);
    }

    [Fact]
    public void PrintLedgerLineScenarioReport_Csv()
    {
        Console.WriteLine("CSV_START");
        Console.WriteLine("Prorate,Billing,Arrival,Departure,Start,End,RentalDescription,Days,Amount,CostCode");

        foreach (var scenario in LedgerLineScenarioMatrix())
        {
            var prorateType = (ProrateType)scenario[0];
            var billingType = (BillingType)scenario[1];
            var arrival = (DateOnly)scenario[2];
            var departure = (DateOnly)scenario[3];
            var startDate = (DateOnly)scenario[4];
            var endDate = (DateOnly)scenario[5];
            var expectedDays = (int)scenario[7];

            var reservation = CreateReservation(
                arrival: arrival,
                departure: departure,
                prorateType: prorateType,
                billingType: billingType,
                billingRate: GetBillingRate(billingType));
            var manager = CreateManager();

            var lines = manager.GetLedgerLinesByReservationIdAsync(
                reservation,
                startDate: startDate,
                endDate: endDate,
                rentalCostCodeId: 77);
            var rental = Assert.Single(lines, x => x.Description.StartsWith("Rental Fee"));

            Console.WriteLine(
                $"{prorateType},{billingType},{arrival:MM/dd/yyyy},{departure:MM/dd/yyyy},{startDate:MM/dd/yyyy},{endDate:MM/dd/yyyy},\"{rental.Description}\",{expectedDays},{rental.Amount:0.##},{rental.CostCodeId}");
        }

        Console.WriteLine("CSV_END");
    }

    #endregion

    #region Private Helpers

    private static AccountingManager CreateManager()
    {
        return new AccountingManager(
            organizationRepository: null!,
            propertyRepository: null!,
            accountingRepository: null!,
            reservationRepository: null!);
    }

    private static decimal GetBillingRate(BillingType billingType)
        => billingType == BillingType.Monthly ? 3000m : 100m;

    private static Reservation CreateReservation(DateOnly arrival, DateOnly departure, ProrateType prorateType, BillingType billingType, decimal billingRate)
    {
        return new Reservation
        {
            ArrivalDate = arrival,
            DepartureDate = departure,
            BillingType = billingType,
            ProrateType = prorateType,
            BillingRate = billingRate,
            DepositType = DepositType.CLR,
            DepartureFee = -1m,
            HasPets = false,
            MaidStartDate = new DateOnly(2100, 1, 1),
            Frequency = FrequencyType.Monthly,
            ExtraFeeLines = []
        };
    }

    #endregion
}

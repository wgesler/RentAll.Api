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
    public void GetLedgerLines_AllRequestedCombinations_Matrix(string caseId, ProrateType prorateType, BillingType billingType, DateOnly arrival, DateOnly departure, DateOnly startDate, DateOnly endDate, string expectedDescription, int expectedDays, decimal expectedAmount, int expectedCostCodeId)
        => AssertLedgerLineScenario("Ledger", caseId, prorateType, billingType, arrival, departure, startDate, endDate, expectedDescription, expectedDays, expectedAmount, expectedCostCodeId);

    [Theory]
    [MemberData(nameof(LeapYearScenarioMatrix))]
    public void GetLedgerLines_FebruaryAndLeapScenarios_Matrix(string caseId, ProrateType prorateType, BillingType billingType, DateOnly arrival, DateOnly departure, DateOnly startDate, DateOnly endDate, string expectedDescription, int expectedDays, decimal expectedAmount, int expectedCostCodeId)
        => AssertLedgerLineScenario("LeapYear", caseId, prorateType, billingType, arrival, departure, startDate, endDate, expectedDescription, expectedDays, expectedAmount, expectedCostCodeId);

    [Theory]
    [MemberData(nameof(CrossYearScenarioMatrix))]
    public void GetLedgerLines_CrossYearScenarios_Matrix(string caseId, ProrateType prorateType, BillingType billingType, DateOnly arrival, DateOnly departure, DateOnly startDate, DateOnly endDate, string expectedDescription, int expectedDays, decimal expectedAmount, int expectedCostCodeId)
        => AssertLedgerLineScenario("CrossYear", caseId, prorateType, billingType, arrival, departure, startDate, endDate, expectedDescription, expectedDays, expectedAmount, expectedCostCodeId);

    private static void AssertLedgerLineScenario(string matrixName, string caseId, ProrateType prorateType, BillingType billingType, DateOnly arrival, DateOnly departure, DateOnly startDate, DateOnly endDate, string expectedDescription, int expectedDays, decimal expectedAmount, int expectedCostCodeId)
    {
        Console.WriteLine(
            $"{matrixName} Case {caseId}: {prorateType},{billingType},{arrival:MM/dd/yyyy},{departure:MM/dd/yyyy},{startDate:MM/dd/yyyy},{endDate:MM/dd/yyyy}");
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
            "1,FirstMonth,Monthly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "2,FirstMonth,Monthly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-04/30),29,2900,77",
            "3,FirstMonth,Monthly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-04/30),26,2600,77",
            "4,FirstMonth,Monthly,04/01/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/01-05/31),31,3000,0",
            "5,FirstMonth,Monthly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,77",
            "6,FirstMonth,Monthly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "7,FirstMonth,Monthly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3000,0",
            "8,FirstMonth,Daily,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "9,FirstMonth,Daily,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-04/30),29,2900,0",
            "10,FirstMonth,Daily,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-04/30),26,2600,0",
            "11,FirstMonth,Daily,04/01/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/01-05/31),31,3100,0",
            "12,FirstMonth,Daily,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,0",
            "13,FirstMonth,Daily,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "14,FirstMonth,Daily,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3100,0",
            "15,FirstMonth,Nightly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "16,FirstMonth,Nightly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-04/30),29,2900,0",
            "17,FirstMonth,Nightly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-04/30),26,2600,0",
            "18,FirstMonth,Nightly,04/01/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/01-05/31),31,3100,0",
            "19,FirstMonth,Nightly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/14),14,1400,0",
            "20,FirstMonth,Nightly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/29),29,2900,0",
            "21,FirstMonth,Nightly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/30),30,3000,0",

            "22,SecondMonth,Monthly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "23,SecondMonth,Monthly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-05/01),30,3000,0",
            "24,SecondMonth,Monthly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-05/04),30,3000,0",
            "25,SecondMonth,Monthly,04/02/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/02-05/31),30,3000,0",
            "26,SecondMonth,Monthly,04/05/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/31),27,2700,0",
            "27,SecondMonth,Monthly,04/05/2026,05/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/15),11,1100,0",
            "28,SecondMonth,Monthly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,77",
            "29,SecondMonth,Monthly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "30,SecondMonth,Monthly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3000,0",
            "31,SecondMonth,Daily,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "32,SecondMonth,Daily,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-05/01),30,3000,0",
            "33,SecondMonth,Daily,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-05/04),30,3000,0",
            "34,SecondMonth,Daily,04/02/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/02-05/31),30,3000,0",
            "35,SecondMonth,Daily,04/05/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/31),27,2700,0",
            "36,SecondMonth,Daily,04/05/2026,05/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/15),11,1100,0",
            "37,SecondMonth,Daily,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/15),15,1500,0",
            "38,SecondMonth,Daily,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/30),30,3000,0",
            "39,SecondMonth,Daily,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/31),31,3100,0",
            "40,SecondMonth,Nightly,04/01/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/01-04/30),30,3000,0",
            "41,SecondMonth,Nightly,04/02/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/02-05/01),30,3000,0",
            "42,SecondMonth,Nightly,04/05/2026,06/15/2026,04/01/2026,04/30/2026,Rental Fee (04/05-05/04),30,3000,0",
            "43,SecondMonth,Nightly,04/02/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/02-05/31),30,3000,0",
            "44,SecondMonth,Nightly,04/05/2026,06/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/31),27,2700,0",
            "45,SecondMonth,Nightly,04/05/2026,05/15/2026,05/01/2026,05/31/2026,Rental Fee (05/05-05/14),10,1000,0",
            "46,SecondMonth,Nightly,04/01/2026,06/15/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/14),14,1400,0",
            "47,SecondMonth,Nightly,04/01/2026,06/30/2026,06/01/2026,06/30/2026,Rental Fee (06/01-06/29),29,2900,0",
            "48,SecondMonth,Nightly,04/01/2026,07/31/2026,07/01/2026,07/31/2026,Rental Fee (07/01-07/30),30,3000,0",

        };

        foreach (var row in rows)
        {
            var cols = row.Split(',');
            yield return new object[]
            {
                cols[0],
                Enum.Parse<ProrateType>(cols[1]),
                Enum.Parse<BillingType>(cols[2]),
                DateOnly.ParseExact(cols[3], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[4], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[5], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[6], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                cols[7],
                int.Parse(cols[8], CultureInfo.InvariantCulture),
                decimal.Parse(cols[9], CultureInfo.InvariantCulture),
                int.Parse(cols[10], CultureInfo.InvariantCulture)
            };
        }
    }

    public static IEnumerable<object[]> LeapYearScenarioMatrix()
    {
        var rows = new[]
        {
            "1,FirstMonth,Monthly,02/01/2026,03/02/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "2,FirstMonth,Monthly,02/01/2026,03/02/2026,03/01/2026,03/02/2026,Rental Fee (03/01-03/02),2,200,0",
            "3,FirstMonth,Monthly,02/04/2026,03/31/2026,02/04/2026,02/28/2026,Rental Fee (02/04-02/28),25,2500,0",
            "4,FirstMonth,Monthly,02/01/2024,03/01/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,3000,0",
            "5,FirstMonth,Monthly,01/01/2024,03/31/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,3000,0",
            "6,FirstMonth,Monthly,02/04/2024,03/31/2024,02/04/2024,02/29/2024,Rental Fee (02/04-02/29),26,2600,0",
            "7,FirstMonth,Monthly,02/01/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "8,FirstMonth,Monthly,01/31/2026,04/30/2026,01/31/2026,01/31/2026,Rental Fee (01/31-01/31),1,100,0",
            "9,FirstMonth,Monthly,01/31/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "10,FirstMonth,Monthly,01/31/2026,04/30/2026,03/01/2026,03/31/2026,Rental Fee (03/01-03/31),31,3000,0",
            "11,FirstMonth,Monthly,02/01/2028,04/30/2028,02/01/2028,02/29/2028,Rental Fee (02/01-02/29),29,3000,0",
            "12,FirstMonth,Monthly,01/31/2026,03/16/2026,03/01/2026,03/16/2026,Rental Fee (03/01-03/16),16,1600,0",
            "13,FirstMonth,Daily,02/01/2026,03/02/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "14,FirstMonth,Daily,02/04/2026,03/31/2026,02/04/2026,02/28/2026,Rental Fee (02/04-02/28),25,2500,0",
            "15,FirstMonth,Daily,02/01/2024,03/01/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,2900,0",
            "16,FirstMonth,Daily,01/01/2024,03/31/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,2900,0",
            "17,FirstMonth,Daily,02/04/2024,03/31/2024,02/04/2024,02/29/2024,Rental Fee (02/04-02/29),26,2600,0",
            "18,FirstMonth,Daily,02/01/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "19,FirstMonth,Daily,01/31/2026,04/30/2026,01/31/2026,01/31/2026,Rental Fee (01/31-01/31),1,100,0",
            "20,FirstMonth,Daily,01/31/2026,04/30/2026,03/01/2026,03/31/2026,Rental Fee (03/01-03/31),31,3100,0",
            "21,FirstMonth,Daily,02/01/2028,04/30/2028,02/01/2028,02/29/2028,Rental Fee (02/01-02/29),29,2900,0",
            "22,FirstMonth,Daily,01/31/2026,03/15/2026,03/01/2026,03/16/2026,Rental Fee (03/01-03/15),15,1500,0",
            "23,FirstMonth,Nightly,02/01/2026,03/02/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "24,FirstMonth,Nightly,02/04/2026,03/31/2026,02/04/2026,02/28/2026,Rental Fee (02/04-02/28),25,2500,0",
            "25,FirstMonth,Nightly,02/01/2024,03/01/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,2900,0",
            "26,FirstMonth,Nightly,01/01/2024,03/31/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,2900,0",
            "27,FirstMonth,Nightly,02/04/2024,03/31/2024,02/04/2024,02/29/2024,Rental Fee (02/04-02/29),26,2600,0",
            "28,FirstMonth,Nightly,02/01/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "29,FirstMonth,Nightly,01/31/2026,04/30/2026,01/31/2026,01/31/2026,Rental Fee (01/31-01/31),1,100,0",
            "30,FirstMonth,Nightly,01/31/2026,04/30/2026,03/01/2026,03/31/2026,Rental Fee (03/01-03/31),31,3100,0",
            "31,FirstMonth,Nightly,02/01/2028,04/30/2028,02/01/2028,02/29/2028,Rental Fee (02/01-02/29),29,2900,0",
            "32,FirstMonth,Nightly,01/31/2026,03/15/2026,03/01/2026,03/16/2026,Rental Fee (03/01-03/14),14,1400,0",
            "33,SecondMonth,Monthly,01/01/2026,03/31/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "34,SecondMonth,Monthly,01/01/2026,02/28/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "35,SecondMonth,Monthly,01/05/2026,02/28/2026,01/01/2026,01/31/2026,Rental Fee (01/05-02/03),30,3000,0",
            "36,SecondMonth,Monthly,01/05/2026,02/28/2026,02/01/2026,02/28/2026,Rental Fee (02/04-02/28),25,2500,0",
            "37,SecondMonth,Monthly,01/05/2024,02/29/2024,02/01/2024,02/29/2024,Rental Fee (02/04-02/29),26,2600,0",
            "38,SecondMonth,Monthly,02/01/2026,03/02/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "39,SecondMonth,Monthly,02/04/2026,03/31/2026,02/04/2026,02/28/2026,Rental Fee (02/04-03/05),30,3000,0",
            "40,SecondMonth,Monthly,02/04/2026,03/31/2026,03/01/2026,03/31/2026,Rental Fee (03/06-03/31),26,2600,0",
            "41,SecondMonth,Monthly,02/01/2024,03/01/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,3000,0",
            "42,SecondMonth,Monthly,01/01/2024,03/31/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,3000,0",
            "43,SecondMonth,Monthly,02/04/2024,03/31/2024,02/04/2024,02/29/2024,Rental Fee (02/04-03/04),30,3000,0",
            "44,SecondMonth,Monthly,02/01/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,3000,0",
            "45,SecondMonth,Monthly,01/31/2026,04/30/2026,01/31/2026,01/31/2026,Rental Fee (01/31-03/01),30,3000,0",
            "46,SecondMonth,Monthly,01/31/2026,04/30/2026,03/01/2026,03/31/2026,Rental Fee (03/02-03/31),30,3000,0",
            "47,SecondMonth,Monthly,02/01/2028,04/30/2028,02/01/2028,02/29/2028,Rental Fee (02/01-02/29),29,3000,0",
            "48,SecondMonth,Monthly,01/31/2026,03/16/2026,03/01/2026,03/16/2026,Rental Fee (03/02-03/16),15,1500,0",
            "49,SecondMonth,Daily,01/01/2026,03/31/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "50,SecondMonth,Daily,01/01/2026,02/28/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "51,SecondMonth,Daily,01/05/2026,02/28/2026,01/01/2026,01/31/2026,Rental Fee (01/05-02/03),30,3000,0",
            "52,SecondMonth,Daily,01/05/2026,02/28/2026,02/01/2026,02/28/2026,Rental Fee (02/04-02/28),25,2500,0",
            "53,SecondMonth,Daily,01/05/2024,02/29/2024,02/01/2024,02/29/2024,Rental Fee (02/04-02/29),26,2600,0",
            "54,SecondMonth,Daily,02/01/2026,03/02/2026,02/01/2026,02/28/2026,Rental Fee (02/01-03/02),30,3000,0",
            "55,SecondMonth,Daily,02/04/2026,03/31/2026,02/04/2026,02/28/2026,Rental Fee (02/04-03/05),30,3000,0",
            "56,SecondMonth,Daily,02/04/2026,03/31/2026,03/01/2026,03/31/2026,Rental Fee (03/06-03/31),26,2600,0",
            "57,SecondMonth,Daily,02/01/2024,03/01/2024,02/01/2024,02/29/2024,Rental Fee (02/01-03/01),30,3000,0",
            "58,SecondMonth,Daily,02/01/2024,03/15/2024,03/01/2024,03/15/2024,Rental Fee (03/02-03/15),14,1400,0",
            "59,SecondMonth,Daily,01/01/2024,03/31/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,2900,0",
            "60,SecondMonth,Daily,01/01/2024,03/31/2024,03/01/2024,03/31/2024,Rental Fee (03/01-03/31),31,3100,0",
            "61,SecondMonth,Daily,02/04/2024,03/31/2024,02/04/2024,02/29/2024,Rental Fee (02/04-03/04),30,3000,0",
            "62,SecondMonth,Daily,02/04/2024,03/31/2024,03/01/2024,03/31/2024,Rental Fee (03/05-03/31),27,2700,0",
            "63,SecondMonth,Daily,02/01/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-03/02),30,3000,0",
            "64,SecondMonth,Daily,01/31/2026,04/30/2026,01/31/2026,01/31/2026,Rental Fee (01/31-03/01),30,3000,0",
            "65,SecondMonth,Daily,01/31/2026,04/30/2026,03/01/2026,03/31/2026,Rental Fee (03/02-03/31),30,3000,0",
            "66,SecondMonth,Daily,02/01/2028,04/30/2028,02/01/2028,02/29/2028,Rental Fee (02/01-03/01),30,3000,0",
            "67,SecondMonth,Daily,01/31/2026,03/16/2026,03/01/2026,03/16/2026,Rental Fee (03/02-03/16),15,1500,0",
            "68,SecondMonth,Nightly,01/01/2026,03/31/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/28),28,2800,0",
            "69,SecondMonth,Nightly,01/01/2026,02/28/2026,02/01/2026,02/28/2026,Rental Fee (02/01-02/27),27,2700,0",
            "70,SecondMonth,Nightly,01/05/2026,02/28/2026,01/01/2026,01/31/2026,Rental Fee (01/05-02/03),30,3000,0",
            "71,SecondMonth,Nightly,01/05/2026,02/28/2026,02/01/2026,02/28/2026,Rental Fee (02/04-02/27),24,2400,0",
            "72,SecondMonth,Nightly,01/05/2024,02/29/2024,02/01/2024,02/29/2024,Rental Fee (02/04-02/28),25,2500,0",
            "73,SecondMonth,Nightly,02/01/2026,03/03/2026,02/01/2026,02/28/2026,Rental Fee (02/01-03/02),30,3000,0",
            "74,SecondMonth,Nightly,02/04/2026,03/31/2026,02/04/2026,02/28/2026,Rental Fee (02/04-03/05),30,3000,0",
            "75,SecondMonth,Nightly,02/04/2026,03/31/2026,03/01/2026,03/31/2026,Rental Fee (03/06-03/30),25,2500,0",
            "76,SecondMonth,Nightly,02/01/2024,03/02/2024,02/01/2024,02/29/2024,Rental Fee (02/01-03/01),30,3000,0",
            "77,SecondMonth,Nightly,02/01/2024,03/15/2024,03/01/2024,03/15/2024,Rental Fee (03/02-03/14),13,1300,0",
            "78,SecondMonth,Nightly,01/01/2024,03/31/2024,02/01/2024,02/29/2024,Rental Fee (02/01-02/29),29,2900,0",
            "79,SecondMonth,Nightly,01/01/2024,03/31/2024,03/01/2024,03/31/2024,Rental Fee (03/01-03/30),30,3000,0",
            "80,SecondMonth,Nightly,02/04/2024,03/31/2024,02/04/2024,02/29/2024,Rental Fee (02/04-03/04),30,3000,0",
            "81,SecondMonth,Nightly,02/04/2024,03/31/2024,03/01/2024,03/31/2024,Rental Fee (03/05-03/30),26,2600,0",
            "82,SecondMonth,Nightly,02/01/2026,04/30/2026,02/01/2026,02/28/2026,Rental Fee (02/01-03/02),30,3000,0",
            "83,SecondMonth,Nightly,01/31/2026,04/30/2026,01/31/2026,01/31/2026,Rental Fee (01/31-03/01),30,3000,0",
            "84,SecondMonth,Nightly,01/31/2026,04/30/2026,03/01/2026,03/31/2026,Rental Fee (03/02-03/31),30,3000,0",
            "85,SecondMonth,Nightly,02/01/2028,04/30/2028,02/01/2028,02/29/2028,Rental Fee (02/01-03/01),30,3000,0",
            "86,SecondMonth,Nightly,01/31/2026,03/16/2026,03/01/2026,03/16/2026,Rental Fee (03/02-03/15),14,1400,0",
        };

        foreach (var row in rows)
        {
            var cols = row.Split(',');
            yield return new object[]
            {
                cols[0],
                Enum.Parse<ProrateType>(cols[1]),
                Enum.Parse<BillingType>(cols[2]),
                DateOnly.ParseExact(cols[3], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[4], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[5], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[6], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                cols[7],
                int.Parse(cols[8], CultureInfo.InvariantCulture),
                decimal.Parse(cols[9], CultureInfo.InvariantCulture),
                int.Parse(cols[10], CultureInfo.InvariantCulture)
            };
        }
    }

    public static IEnumerable<object[]> CrossYearScenarioMatrix()
    {
        var rows = new[]
        {
            "1,FirstMonth,Monthly,12/15/2025,03/02/2026,12/01/2025,12/31/2025,Rental Fee (12/15-12/31),17,1700,0",
            "2,SecondMonth,Monthly,12/15/2025,03/02/2026,12/01/2025,12/31/2025,Rental Fee (12/15-01/13),30,3000,0",
            "3,FirstMonth,Daily,12/15/2025,03/02/2026,12/01/2025,12/31/2025,Rental Fee (12/15-12/31),17,1700,0",
            "4,SecondMonth,Daily,12/15/2025,03/02/2026,12/01/2025,12/31/2025,Rental Fee (12/15-01/13),30,3000,0",
            "5,FirstMonth,Nightly,12/15/2025,03/02/2026,12/01/2025,12/31/2025,Rental Fee (12/15-12/31),17,1700,0",
            "6,SecondMonth,Nightly,12/15/2025,03/02/2026,12/01/2025,12/31/2025,Rental Fee (12/15-01/13),30,3000,0",
        };

        foreach (var row in rows)
        {
            var cols = row.Split(',');
            yield return new object[]
            {
                cols[0],
                Enum.Parse<ProrateType>(cols[1]),
                Enum.Parse<BillingType>(cols[2]),
                DateOnly.ParseExact(cols[3], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[4], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[5], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(cols[6], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                cols[7],
                int.Parse(cols[8], CultureInfo.InvariantCulture),
                decimal.Parse(cols[9], CultureInfo.InvariantCulture),
                int.Parse(cols[10], CultureInfo.InvariantCulture)
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
        rows.Add("| Case | Prorate | Billing | Arrival | Departure | Start | End | Rental Description | Days | Amount | CostCode |");
        rows.Add("|---:|---|---|---|---|---|---|---|---:|---:|---:|");

        foreach (var scenario in LedgerLineScenarioMatrix())
        {
            var caseId = (string)scenario[0];
            var prorateType = (ProrateType)scenario[1];
            var billingType = (BillingType)scenario[2];
            var arrival = (DateOnly)scenario[3];
            var departure = (DateOnly)scenario[4];
            var startDate = (DateOnly)scenario[5];
            var endDate = (DateOnly)scenario[6];
            var expectedDays = (int)scenario[8];

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
                $"| {caseId} | {prorateType} | {billingType} | {arrival:MM/dd/yyyy} | {departure:MM/dd/yyyy} | {startDate:MM/dd/yyyy} | {endDate:MM/dd/yyyy} | {rental.Description} | {expectedDays} | {rental.Amount:0.##} | {rental.CostCodeId} |");
        }

        Assert.NotEmpty(rows);
        foreach (var row in rows)
            Console.WriteLine(row);
    }

    [Fact]
    public void PrintLedgerLineScenarioReport_Csv()
    {
        Console.WriteLine("CSV_START");
        Console.WriteLine("Case,Prorate,Billing,Arrival,Departure,Start,End,RentalDescription,Days,Amount,CostCode");

        foreach (var scenario in LedgerLineScenarioMatrix())
        {
            var caseId = (string)scenario[0];
            var prorateType = (ProrateType)scenario[1];
            var billingType = (BillingType)scenario[2];
            var arrival = (DateOnly)scenario[3];
            var departure = (DateOnly)scenario[4];
            var startDate = (DateOnly)scenario[5];
            var endDate = (DateOnly)scenario[6];
            var expectedDays = (int)scenario[8];

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
                $"{caseId},{prorateType},{billingType},{arrival:MM/dd/yyyy},{departure:MM/dd/yyyy},{startDate:MM/dd/yyyy},{endDate:MM/dd/yyyy},\"{rental.Description}\",{expectedDays},{rental.Amount:0.##},{rental.CostCodeId}");
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

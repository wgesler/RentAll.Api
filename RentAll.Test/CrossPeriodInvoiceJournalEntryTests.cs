using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class CrossPeriodInvoiceJournalEntryTests
{
    // Weekly maid from Feb 25: partial first slice (through cross-month rent end) → 2 visits;
    // full March window → 4 visits.
    private static readonly DateOnly FebArrival = new(2026, 2, 4);
    private static readonly DateOnly FebMaidStart = new(2026, 2, 25);
    private static readonly DateOnly FebSliceStart = new(2026, 2, 1);
    private static readonly DateOnly FebSliceEnd = new(2026, 2, 28);
    private static readonly DateOnly MarSliceStart = new(2026, 3, 1);
    private static readonly DateOnly MarSliceEnd = new(2026, 3, 31);
    private static readonly DateOnly MarSliceEndForCrossMonthRent = new(2026, 3, 14);

    // Weekly maid from Jan 22 → Jan 22 & 29 (2), then Feb 5, 12, 19 & 26 (4) in a full February window.
    private static readonly DateOnly JanArrival = new(2026, 1, 15);
    private static readonly DateOnly JanMaidStart = new(2026, 1, 22);
    private static readonly DateOnly JanSliceStart = new(2026, 1, 15);
    private static readonly DateOnly JanSliceEnd = new(2026, 1, 31);
    private static readonly DateOnly FebShortSliceStart = new(2026, 2, 1);
    private static readonly DateOnly FebShortSliceEnd = new(2026, 2, 14);
    private static readonly DateOnly FebFullSliceEnd = new(2026, 2, 28);

    [Fact]
    public void CrossPeriodSplit_SecondMonthProrate_FebInvoiceWithCrossMonthRent_TotalsDoNotReconcile()
    {
        var reservation = CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            FebMaidStart);

        AssertCrossPeriodSplitDoesNotReconcile(
            reservation,
            originalStart: FebSliceStart,
            originalEnd: FebSliceEnd,
            slice1Start: FebSliceStart,
            slice1End: FebSliceEnd,
            slice2Start: MarSliceStart,
            slice2End: MarSliceEndForCrossMonthRent);
    }

    [Fact]
    public void CrossPeriodSplit_InvoicePeriodSpansTwoMonths_TotalsDoNotReconcileDueToFirstMonthFees()
    {
        var reservation = CreateReservationWithFees(
            JanArrival,
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            JanMaidStart);

        AssertCrossPeriodSplitDoesNotReconcile(
            reservation,
            originalStart: JanSliceStart,
            originalEnd: FebShortSliceEnd,
            slice1Start: JanSliceStart,
            slice1End: JanSliceEnd,
            slice2Start: FebShortSliceStart,
            slice2End: FebShortSliceEnd);
    }

    [Fact]
    public void CrossPeriodSplit_SecondMonthProrate_FirstMonthFeesAndMaid_OnlyOnFirstSlice()
    {
        var reservation = CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            FebMaidStart);

        var manager = CreateManager();
        var slice1Lines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);
        var slice2Lines = GetLines(manager, reservation, MarSliceStart, MarSliceEnd);

        AssertLineAmount(slice1Lines, "Security Deposit", 500m);
        AssertLineAmount(slice1Lines, "Pet Fee", 250m);
        AssertMaidServiceCount(slice1Lines, expectedTimes: 2, feePerVisit: 100m);

        AssertDoesNotContainDescription(slice2Lines, "Security Deposit");
        AssertDoesNotContainDescription(slice2Lines, "Pet Fee");
        AssertMaidServiceCountInRange(slice2Lines, minTimes: 3, maxTimes: 4, feePerVisit: 100m);
    }

    [Fact]
    public void CrossPeriodSplit_InvoicePeriodSpansTwoMonths_FirstMonthFeesOnlyOnJanuarySlice()
    {
        var reservation = CreateReservationWithFees(
            JanArrival,
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            JanMaidStart);

        var manager = CreateManager();
        var slice1Lines = GetLines(manager, reservation, JanSliceStart, JanSliceEnd);
        var slice2Lines = GetLines(manager, reservation, FebShortSliceStart, FebFullSliceEnd);

        AssertLineAmount(slice1Lines, "Security Deposit", 500m);
        AssertLineAmount(slice1Lines, "Pet Fee", 250m);
        AssertMaidServiceCount(slice1Lines, expectedTimes: 2, feePerVisit: 100m);

        AssertDoesNotContainDescription(slice2Lines, "Security Deposit");
        AssertDoesNotContainDescription(slice2Lines, "Pet Fee");
        AssertMaidServiceCountInRange(slice2Lines, minTimes: 3, maxTimes: 4, feePerVisit: 100m);
    }

    [Fact]
    public void CrossPeriodSplit_SecondMonthProrate_OriginalInvoiceIncludesDepositPetAndMaid()
    {
        var reservation = CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            FebMaidStart);

        var manager = CreateManager();
        var originalLines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);

        AssertLineAmount(originalLines, "Security Deposit", 500m);
        AssertLineAmount(originalLines, "Pet Fee", 250m);
        AssertMaidServiceCount(originalLines, expectedTimes: 2, feePerVisit: 100m);
        Assert.Contains(originalLines, line => line.Description.StartsWith("Rental Fee (02/04-03/"));
    }

    [Fact]
    public void CrossPeriodSplit_AportionedSdw_Feb4ToMar5_SplitsBySameDailyRateAsRent()
    {
        var reservation = CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            hasPets: false);

        var manager = CreateManager();
        var originalLines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);
        var originalRental = Assert.Single(originalLines, line => line.Description == "Rental Fee (02/04-03/05)");
        var originalSdw = Assert.Single(originalLines, line => line.Description == "Security Deposit Waiver");

        var (firstMonthRent, secondMonthRent) = ApportionCrossMonthRental(originalRental, reservation);
        var (firstMonthSdw, secondMonthSdw) = ApportionCrossMonthFee(originalSdw, originalRental, reservation);

        Assert.Equal(2500m, firstMonthRent);
        Assert.Equal(500m, secondMonthRent);
        Assert.Equal(originalSdw.Amount * firstMonthRent / originalRental.Amount, firstMonthSdw);
        Assert.Equal(originalSdw.Amount * secondMonthRent / originalRental.Amount, secondMonthSdw);
        Assert.Equal(originalSdw.Amount, firstMonthSdw + secondMonthSdw);
    }

    [Fact]
    public void CrossPeriodSplit_AportionedRental_Feb4ToMar5_SplitsByDailyRate()
    {
        var reservation = CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            FebMaidStart);

        var manager = CreateManager();
        var originalLines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);
        var originalRental = Assert.Single(originalLines, line => line.Description == "Rental Fee (02/04-03/05)");
        Assert.Equal(3000m, originalRental.Amount);

        var (firstMonthRent, secondMonthRent) = ApportionCrossMonthRental(originalRental, reservation);

        Assert.Equal(2500m, firstMonthRent);
        Assert.Equal(500m, secondMonthRent);
        Assert.Equal(originalRental.Amount, firstMonthRent + secondMonthRent);
    }

    /// <summary>
    /// Mirrors cross-period rental apportionment: original amount / total rental days × days in each calendar month.
    /// </summary>
    private static (decimal FirstMonthRent, decimal SecondMonthRent) ApportionCrossMonthRental(
        LedgerLine originalRental,
        Reservation reservation)
    {
        const int referenceYear = 2026;
        var match = System.Text.RegularExpressions.Regex.Match(
            originalRental.Description,
            @"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$");
        Assert.True(match.Success);

        var rentalStart = ParseMonthDay(match.Groups["start"].Value, referenceYear);
        var rentalEnd = ParseMonthDay(match.Groups["end"].Value, referenceYear);
        if (rentalEnd < rentalStart)
            rentalEnd = rentalEnd.AddYears(1);

        var departureDate = reservation.DepartureDate;
        var totalDays = CalculateBillingDays(
            rentalStart,
            rentalEnd,
            reservation.BillingType,
            IsDepartureMonthYear(rentalEnd, departureDate),
            IsLastDayOfMonth(rentalEnd));

        var firstMonthEnd = new DateOnly(rentalStart.Year, rentalStart.Month, DateTime.DaysInMonth(rentalStart.Year, rentalStart.Month));
        var secondMonthStart = new DateOnly(rentalEnd.Year, rentalEnd.Month, 1);
        var firstPeriodEnd = rentalEnd < firstMonthEnd ? rentalEnd : firstMonthEnd;

        var firstDays = CalculateBillingDays(
            rentalStart,
            firstPeriodEnd,
            reservation.BillingType,
            IsDepartureMonthYear(firstPeriodEnd, departureDate),
            IsLastDayOfMonth(firstPeriodEnd));

        var dailyRate = originalRental.Amount / totalDays;
        var firstMonthRent = dailyRate * firstDays;
        var secondMonthRent = originalRental.Amount - firstMonthRent;
        return (firstMonthRent, secondMonthRent);
    }

    private static (decimal FirstMonthFee, decimal SecondMonthFee) ApportionCrossMonthFee(
        LedgerLine originalFee,
        LedgerLine originalRental,
        Reservation reservation)
    {
        var (firstMonthRent, _) = ApportionCrossMonthRental(originalRental, reservation);
        var firstMonthFee = originalFee.Amount * firstMonthRent / originalRental.Amount;
        var secondMonthFee = originalFee.Amount - firstMonthFee;
        return (firstMonthFee, secondMonthFee);
    }

    private static DateOnly ParseMonthDay(string monthDay, int referenceYear)
    {
        var parts = monthDay.Split('/');
        return new DateOnly(referenceYear, int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static bool IsDepartureMonthYear(DateOnly date, DateOnly departureDate)
        => date.Year == departureDate.Year && date.Month == departureDate.Month;

    private static bool IsLastDayOfMonth(DateOnly date)
        => date.Day == DateTime.DaysInMonth(date.Year, date.Month);

    private static int CalculateBillingDays(
        DateOnly startDate,
        DateOnly endDate,
        BillingType billingType,
        bool isDepartureMonthYear,
        bool isLastDayOfMonth)
    {
        if (endDate < startDate)
            return 0;
        if (endDate == startDate)
            return 1;

        var days = endDate.DayNumber - startDate.DayNumber;
        if (billingType != BillingType.Nightly ||
            (billingType == BillingType.Nightly && !isDepartureMonthYear && isLastDayOfMonth))
            days++;

        return days;
    }

    private static void AssertCrossPeriodSplitDoesNotReconcile(
        Reservation reservation,
        DateOnly originalStart,
        DateOnly originalEnd,
        DateOnly slice1Start,
        DateOnly slice1End,
        DateOnly slice2Start,
        DateOnly slice2End)
    {
        var manager = CreateManager();

        var originalLines = GetLines(manager, reservation, originalStart, originalEnd);
        var slice1Lines = GetLines(manager, reservation, slice1Start, slice1End);
        var slice2Lines = GetLines(manager, reservation, slice2Start, slice2End);

        var originalTotal = originalLines.Sum(l => l.Amount);
        var splitTotal = slice1Lines.Sum(l => l.Amount) + slice2Lines.Sum(l => l.Amount);

        Assert.Contains(originalLines, line => line.Description == "Security Deposit");
        Assert.Contains(originalLines, line => line.Description == "Pet Fee");
        Assert.Contains(originalLines, line => line.Description.StartsWith("Maid Service"));

        Assert.NotEqual(originalTotal, splitTotal);
        Assert.True(splitTotal > originalTotal);
    }

    private static List<LedgerLine> GetLines(AccountingManager manager, Reservation reservation, DateOnly start, DateOnly end)
        => manager.GetLedgerLinesByReservationIdAsync(reservation, start, end, rentalCostCodeId: 77);

    private static void AssertLineAmount(IEnumerable<LedgerLine> lines, string description, decimal expectedAmount)
    {
        var line = Assert.Single(lines, l => l.Description == description);
        Assert.Equal(expectedAmount, line.Amount);
    }

    private static void AssertDoesNotContainDescription(IEnumerable<LedgerLine> lines, string description)
        => Assert.DoesNotContain(lines, line => line.Description == description);

    private static void AssertMaidServiceCount(IEnumerable<LedgerLine> lines, int expectedTimes, decimal feePerVisit)
    {
        var maidLine = Assert.Single(lines, line => line.Description.StartsWith("Maid Service"));
        Assert.Equal($"Maid Service ({expectedTimes} times)", maidLine.Description);
        Assert.Equal(expectedTimes * feePerVisit, maidLine.Amount);
    }

    private static void AssertMaidServiceCountInRange(
        IEnumerable<LedgerLine> lines,
        int minTimes,
        int maxTimes,
        decimal feePerVisit)
    {
        var maidLine = Assert.Single(lines, line => line.Description.StartsWith("Maid Service"));
        var times = ExtractMaidServiceTimes(maidLine.Description);
        Assert.InRange(times, minTimes, maxTimes);
        Assert.Equal(times * feePerVisit, maidLine.Amount);
    }

    private static int ExtractMaidServiceTimes(string description)
    {
        var open = description.IndexOf('(');
        var close = description.IndexOf(" times)", StringComparison.Ordinal);
        if (open < 0 || close <= open)
            throw new InvalidOperationException($"Unexpected maid service description: {description}");

        return int.Parse(description.Substring(open + 1, close - open - 1));
    }

    private static Reservation CreateReservationWithFees(
        DateOnly arrival,
        DateOnly departure,
        ProrateType prorateType,
        BillingType billingType,
        DateOnly maidStartDate,
        DepositType depositType = DepositType.Deposit,
        bool hasPets = true)
    {
        return new Reservation
        {
            OrganizationId = Guid.NewGuid(),
            OfficeId = 1,
            PropertyId = Guid.NewGuid(),
            ArrivalDate = arrival,
            DepartureDate = departure,
            ProrateType = prorateType,
            BillingType = billingType,
            BillingRate = billingType == BillingType.Monthly ? 3000m : 100m,
            Deposit = 500m,
            DepositType = depositType,
            HasPets = hasPets,
            PetFee = 250m,
            DepartureFee = -1m,
            MaidServiceFee = 100m,
            Frequency = FrequencyType.Weekly,
            MaidStartDate = maidStartDate,
            ExtraFeeLines = []
        };
    }

    private static AccountingManager CreateManager()
    {
        return new AccountingManager(
            organizationRepository: null!,
            propertyRepository: null!,
            accountingRepository: null!,
            maintenanceRepository: null!,
            reservationRepository: null!,
            journalEntryRepository: null!,
            organizationManager: null!,
            featureFlagService: new EnabledFeatureFlagService());
    }

    private sealed class EnabledFeatureFlagService : IFeatureFlagService
    {
        public IReadOnlyDictionary<string, bool> GetAll()
            => new Dictionary<string, bool> { [FeatureFlagKeys.Accounting] = true };

        public bool IsEnabled(string featureName) => true;

        public Task<bool> IsEnabledAsync(string featureName, Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public void Set(string featureName, bool enabled)
        {
        }
    }
}

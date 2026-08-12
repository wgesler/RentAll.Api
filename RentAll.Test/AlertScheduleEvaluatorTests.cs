using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Domain.Scheduling;

namespace RentAll.Test;

public class AlertScheduleEvaluatorTests
{
    [Fact]
    public void IsDue_OneTime_UsesDaysBeforeDepartureBeforeStartDate()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.AddDays(-30).Date),
            departureDate: DateOnly.FromDateTime(utcNow.AddDays(2).Date),
            daysBeforeDeparture: 1);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.False(isDue);
    }

    [Fact]
    public void IsDue_OneTime_IsDueAtDaysBeforeDepartureThreshold()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.Date),
            departureDate: DateOnly.FromDateTime(utcNow.AddDays(7).Date),
            daysBeforeDeparture: 7);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.True(isDue);
    }

    [Fact]
    public void IsDue_OneTime_WithDaysBeforeDepartureButNoDepartureDate_IsNotDue()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.AddDays(-1).Date),
            departureDate: null,
            daysBeforeDeparture: 3);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.False(isDue);
    }

    [Fact]
    public void IsDue_OneTime_UsesStartDateWhenDaysBeforeDepartureIsNull()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.Date),
            departureDate: DateOnly.FromDateTime(utcNow.AddDays(14).Date),
            daysBeforeDeparture: null);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.True(isDue);
    }

    [Fact]
    public void IsDue_OneTime_AlreadySucceeded_IsNotDue()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.AddDays(-1).Date),
            departureDate: DateOnly.FromDateTime(utcNow.AddDays(1).Date),
            daysBeforeDeparture: 1);
        alert.EmailStatus = EmailStatus.Succeeded;
        alert.SentOn = utcNow;

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.False(isDue);
    }

    [Fact]
    public void GetNextAlertDate_OneTime_WithDaysBeforeDeparture_ReturnsDepartureMinusDays()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.AddDays(-30).Date),
            departureDate: DateOnly.FromDateTime(utcNow.AddDays(5).Date),
            daysBeforeDeparture: 2);

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, utcNow);

        Assert.Equal(new DateOnly(2026, 04, 17), nextAlertDate);
    }

    [Fact]
    public void GetNextAlertDate_OneTime_AlreadySucceeded_ReturnsNull()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: DateOnly.FromDateTime(utcNow.AddDays(-1).Date),
            departureDate: DateOnly.FromDateTime(utcNow.AddDays(1).Date),
            daysBeforeDeparture: 1);
        alert.EmailStatus = EmailStatus.Succeeded;
        alert.SentOn = utcNow;

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, utcNow);

        Assert.Null(nextAlertDate);
    }

    [Fact]
    public void GetNextAlertDate_RecurringWhenCurrentPeriodAlreadySent_ReturnsNextPeriodStart()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var startDate = DateOnly.FromDateTime(utcNow.AddDays(-10).Date);
        var alert = new Alert
        {
            Frequency = FrequencyType.Weekly,
            StartDate = startDate,
            SentOn = utcNow.AddDays(-1)
        };

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, utcNow);

        Assert.Equal(new DateOnly(2026, 04, 18), nextAlertDate);
    }

    [Fact]
    public void IsDue_Weekly_OnlyOnExactPeriodStartDay()
    {
        // Start 05/08 weekly → period starts include 08/07 and 08/14.
        var startDate = new DateOnly(2026, 05, 08);
        var alert = new Alert
        {
            Frequency = FrequencyType.Weekly,
            StartDate = startDate,
            SentOn = new DateTimeOffset(2026, 07, 31, 12, 0, 0, TimeSpan.Zero)
        };

        Assert.False(AlertScheduleEvaluator.IsDue(alert, new DateTimeOffset(2026, 08, 12, 12, 0, 0, TimeSpan.Zero)));
        Assert.True(AlertScheduleEvaluator.IsDue(alert, new DateTimeOffset(2026, 08, 14, 12, 0, 0, TimeSpan.Zero)));
        Assert.False(AlertScheduleEvaluator.IsDue(alert, new DateTimeOffset(2026, 08, 15, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void IsDue_Weekly_AlreadySentOnPeriodStart_IsNotDueAgainSameDay()
    {
        var alert = new Alert
        {
            Frequency = FrequencyType.Weekly,
            StartDate = new DateOnly(2026, 05, 08),
            SentOn = new DateTimeOffset(2026, 08, 14, 8, 0, 0, TimeSpan.Zero)
        };

        Assert.False(AlertScheduleEvaluator.IsDue(alert, new DateTimeOffset(2026, 08, 14, 18, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void GetNextAlertDate_RecurringWhenPeriodStartMissed_ReturnsNextPeriodStart()
    {
        var alert = new Alert
        {
            Frequency = FrequencyType.Weekly,
            StartDate = new DateOnly(2026, 05, 08),
            SentOn = new DateTimeOffset(2026, 07, 31, 12, 0, 0, TimeSpan.Zero)
        };

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, new DateTimeOffset(2026, 08, 12, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 08, 14), nextAlertDate);
    }

    [Fact]
    public void IsDue_OneTime_MissedExactDay_IsNotDue()
    {
        var utcNow = new DateTimeOffset(2026, 04, 15, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: new DateOnly(2026, 04, 14),
            departureDate: null,
            daysBeforeDeparture: null);

        Assert.False(AlertScheduleEvaluator.IsDue(alert, utcNow));
    }

    private static Alert CreateOneTimeAlert(DateOnly? startDate, DateOnly? departureDate, int? daysBeforeDeparture)
    {
        return new Alert
        {
            Frequency = FrequencyType.OneTime,
            EmailStatus = EmailStatus.Unsent,
            StartDate = startDate,
            DepartureDate = departureDate,
            DaysBeforeDeparture = daysBeforeDeparture
        };
    }
}

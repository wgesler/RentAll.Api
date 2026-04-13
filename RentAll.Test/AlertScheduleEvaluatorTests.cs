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
            startDate: utcNow.AddDays(-30),
            departureDate: utcNow.AddDays(2),
            daysBeforeDeparture: 1);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.False(isDue);
    }

    [Fact]
    public void IsDue_OneTime_IsDueAtDaysBeforeDepartureThreshold()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: utcNow,
            departureDate: utcNow.AddDays(7),
            daysBeforeDeparture: 7);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.True(isDue);
    }

    [Fact]
    public void IsDue_OneTime_WithDaysBeforeDepartureButNoDepartureDate_IsNotDue()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: utcNow.AddDays(-1),
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
            startDate: utcNow.AddHours(-1),
            departureDate: utcNow.AddDays(14),
            daysBeforeDeparture: null);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.True(isDue);
    }

    [Fact]
    public void IsDue_OneTime_AlreadySucceeded_IsNotDue()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: utcNow.AddDays(-1),
            departureDate: utcNow.AddDays(1),
            daysBeforeDeparture: 1);
        alert.EmailStatus = EmailStatus.Succeeded;
        alert.SentOn = utcNow.AddMinutes(-5);

        var isDue = AlertScheduleEvaluator.IsDue(alert, utcNow);

        Assert.False(isDue);
    }

    [Fact]
    public void GetNextAlertDate_OneTime_WithDaysBeforeDeparture_ReturnsDepartureOffsetDate()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: utcNow.AddDays(-30),
            departureDate: utcNow.AddDays(5),
            daysBeforeDeparture: 2);

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, utcNow);

        Assert.Equal(utcNow.AddDays(3), nextAlertDate);
    }

    [Fact]
    public void GetNextAlertDate_OneTime_AlreadySucceeded_ReturnsNull()
    {
        var utcNow = new DateTimeOffset(2026, 04, 14, 12, 0, 0, TimeSpan.Zero);
        var alert = CreateOneTimeAlert(
            startDate: utcNow.AddDays(-1),
            departureDate: utcNow.AddDays(1),
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
        var startDate = utcNow.AddDays(-10);
        var alert = new Alert
        {
            Frequency = FrequencyType.Weekly,
            StartDate = startDate,
            SentOn = utcNow.AddDays(-1)
        };

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, utcNow);

        Assert.Equal(startDate.AddDays(14), nextAlertDate);
    }

    private static Alert CreateOneTimeAlert(DateTimeOffset? startDate, DateTimeOffset? departureDate, int? daysBeforeDeparture)
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

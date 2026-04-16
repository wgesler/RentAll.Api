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
        alert.SentOn = DateOnly.FromDateTime(utcNow.UtcDateTime);

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
        alert.SentOn = DateOnly.FromDateTime(utcNow.UtcDateTime);

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
            SentOn = DateOnly.FromDateTime(utcNow.AddDays(-1).UtcDateTime)
        };

        var nextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, utcNow);

        Assert.Equal(new DateOnly(2026, 04, 18), nextAlertDate);
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

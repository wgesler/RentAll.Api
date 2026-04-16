using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Scheduling;

/// <summary>
/// Decides whether an alert should be sent on this scheduler tick (UTC calendar dates).
/// </summary>
public static class AlertScheduleEvaluator
{
    private static DateOnly UtcCalendarDate(DateTimeOffset utcNow) =>
        DateOnly.FromDateTime(utcNow.UtcDateTime);

    private static DateOnly UtcCalendarDateFromOffset(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.UtcDateTime);

    public static DateOnly? GetNextAlertDate(Alert alert, DateTimeOffset utcNow)
    {
        if (alert.Frequency == FrequencyType.NA)
            return null;

        if (alert.Frequency == FrequencyType.OneTime)
            return GetOneTimeNextAlertDate(alert);

        if (!alert.StartDate.HasValue)
            return null;

        var periodLengthDays = GetPeriodLengthDays(alert.Frequency);
        if (!periodLengthDays.HasValue)
            return null;

        var today = UtcCalendarDate(utcNow);
        var anchor = alert.StartDate.Value;
        if (today < anchor)
            return anchor;

        var elapsedDays = today.DayNumber - anchor.DayNumber;
        var periodIndex = elapsedDays / periodLengthDays.Value;
        var periodStartDate = anchor.AddDays(periodIndex * periodLengthDays.Value);

        if (alert.SentOn.HasValue && UtcCalendarDateFromOffset(alert.SentOn.Value) >= periodStartDate)
            return periodStartDate.AddDays(periodLengthDays.Value);

        return periodStartDate;
    }

    public static bool IsDue(Alert alert, DateTimeOffset utcNow)
    {
        if (alert.Frequency == FrequencyType.NA)
            return false;

        var today = UtcCalendarDate(utcNow);

        // Same UTC calendar day as last attempt while still "Attempting" — avoid hammering SendGrid.
        if (alert.EmailStatus == EmailStatus.Attempting &&
            alert.LastAttemptedOn.HasValue &&
            UtcCalendarDateFromOffset(alert.LastAttemptedOn.Value) == today)
            return false;

        if (alert.Frequency == FrequencyType.OneTime)
            return IsOneTimeDue(alert, today);

        if (!alert.StartDate.HasValue)
            return false;

        var anchor = alert.StartDate.Value;
        if (today < anchor)
            return false;

        return alert.Frequency switch
        {
            FrequencyType.Weekly => IsRecurringPeriodDue(anchor, today, alert.SentOn, 7),
            FrequencyType.EOW => IsRecurringPeriodDue(anchor, today, alert.SentOn, 14),
            FrequencyType.Monthly => IsRecurringPeriodDue(anchor, today, alert.SentOn, 30),
            FrequencyType.Quarterly => IsRecurringPeriodDue(anchor, today, alert.SentOn, 90),
            FrequencyType.BiAnnually => IsRecurringPeriodDue(anchor, today, alert.SentOn, 180),
            FrequencyType.Annually => IsRecurringPeriodDue(anchor, today, alert.SentOn, 365),
            _ => false
        };
    }

    private static bool IsOneTimeDue(Alert alert, DateOnly today)
    {
        if (alert.SentOn.HasValue || alert.EmailStatus == EmailStatus.Succeeded)
            return false;

        if (!TryGetOneTimeTriggerDate(alert, out var triggerDate))
            return false;

        if (today < triggerDate)
            return false;

        return alert.EmailStatus is EmailStatus.Unsent or EmailStatus.Failed;
    }

    private static DateOnly? GetOneTimeNextAlertDate(Alert alert)
    {
        if (alert.SentOn.HasValue || alert.EmailStatus == EmailStatus.Succeeded)
            return null;

        return TryGetOneTimeTriggerDate(alert, out var triggerDate) ? triggerDate : null;
    }

    private static bool TryGetOneTimeTriggerDate(Alert alert, out DateOnly triggerDate)
    {
        triggerDate = default;

        if (alert.DaysBeforeDeparture.HasValue)
        {
            if (alert.DaysBeforeDeparture.Value < 0 || !alert.DepartureDate.HasValue)
                return false;

            triggerDate = alert.DepartureDate.Value.AddDays(-alert.DaysBeforeDeparture.Value);
            return true;
        }

        if (!alert.StartDate.HasValue)
            return false;

        triggerDate = alert.StartDate.Value;
        return true;
    }

    private static int? GetPeriodLengthDays(FrequencyType frequency)
    {
        return frequency switch
        {
            FrequencyType.Weekly => 7,
            FrequencyType.EOW => 14,
            FrequencyType.Monthly => 30,
            FrequencyType.Quarterly => 90,
            FrequencyType.BiAnnually => 180,
            FrequencyType.Annually => 365,
            _ => null
        };
    }

    /// <summary>
    /// Due if we are on or after the start of the current period and <see cref="Alert.SentOn"/> is before that period start.
    /// Uses fixed day counts (approximate calendar months/years).
    /// </summary>
    private static bool IsRecurringPeriodDue(DateOnly anchorDate, DateOnly today, DateTimeOffset? sentOn, int periodLengthDays)
    {
        if (periodLengthDays <= 0)
            return false;

        var elapsedDays = today.DayNumber - anchorDate.DayNumber;
        if (elapsedDays < 0)
            return false;

        var periodIndex = elapsedDays / periodLengthDays;
        var periodStartDate = anchorDate.AddDays(periodIndex * periodLengthDays);

        if (today < periodStartDate)
            return false;

        return !sentOn.HasValue || UtcCalendarDateFromOffset(sentOn.Value) < periodStartDate;
    }
}

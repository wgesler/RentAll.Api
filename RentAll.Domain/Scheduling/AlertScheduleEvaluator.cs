using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Scheduling;

/// <summary>
/// Decides whether an alert should be sent on this scheduler tick (UTC).
/// </summary>
public static class AlertScheduleEvaluator
{
    private static readonly TimeSpan AttemptingCooldown = TimeSpan.FromMinutes(55);

    public static DateTimeOffset? GetNextAlertDate(Alert alert, DateTimeOffset utcNow)
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

        var anchor = alert.StartDate.Value;
        if (utcNow < anchor)
            return anchor;

        var elapsed = utcNow - anchor;
        var periodIndex = (int)(elapsed.TotalDays / periodLengthDays.Value);
        var periodStart = anchor.AddDays(periodIndex * periodLengthDays.Value);

        if (alert.SentOn.HasValue && alert.SentOn.Value >= periodStart)
            return periodStart.AddDays(periodLengthDays.Value);

        return periodStart;
    }

    public static bool IsDue(Alert alert, DateTimeOffset utcNow)
    {
        if (alert.Frequency == FrequencyType.NA)
            return false;

        if (alert.EmailStatus == EmailStatus.Attempting && alert.LastAttemptedOn.HasValue && utcNow - alert.LastAttemptedOn.Value < AttemptingCooldown)
            return false;

        if (alert.Frequency == FrequencyType.OneTime)
            return IsOneTimeDue(alert, utcNow);

        if (!alert.StartDate.HasValue)
            return false;

        var start = alert.StartDate.Value;
        if (utcNow < start)
            return false;

        return alert.Frequency switch
        {
            FrequencyType.Weekly => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 7),
            FrequencyType.EOW => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 14),
            FrequencyType.Monthly => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 30),
            FrequencyType.Quarterly => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 90),
            FrequencyType.BiAnnually => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 180),
            FrequencyType.Annually => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 365),
            _ => false
        };
    }

    private static bool IsOneTimeDue(Alert alert, DateTimeOffset utcNow)
    {
        if (alert.SentOn.HasValue || alert.EmailStatus == EmailStatus.Succeeded)
            return false;

        if (!TryGetOneTimeTriggerDate(alert, out var triggerDate))
            return false;

        if (utcNow < triggerDate)
            return false;

        return alert.EmailStatus is EmailStatus.Unsent or EmailStatus.Failed;
    }

    private static DateTimeOffset? GetOneTimeNextAlertDate(Alert alert)
    {
        if (alert.SentOn.HasValue || alert.EmailStatus == EmailStatus.Succeeded)
            return null;

        return TryGetOneTimeTriggerDate(alert, out var triggerDate) ? triggerDate : null;
    }

    private static bool TryGetOneTimeTriggerDate(Alert alert, out DateTimeOffset triggerDate)
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
    /// Due if we are inside a new period that has not yet been satisfied by <see cref="Alert.SentOn"/>.
    /// Uses fixed day counts (approximate calendar months/years).
    /// </summary>
    private static bool IsRecurringPeriodDue(DateTimeOffset anchor, DateTimeOffset utcNow, DateTimeOffset? sentOn, int periodLengthDays)
    {
        if (periodLengthDays <= 0)
            return false;

        var elapsed = utcNow - anchor;
        if (elapsed < TimeSpan.Zero)
            return false;

        var periodIndex = (int)(elapsed.TotalDays / periodLengthDays);
        var periodStart = anchor.AddDays(periodIndex * periodLengthDays);

        if (utcNow < periodStart)
            return false;

        return !sentOn.HasValue || sentOn.Value < periodStart;
    }
}

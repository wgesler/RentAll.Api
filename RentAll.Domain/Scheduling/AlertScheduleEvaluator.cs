using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Scheduling;

/// <summary>
/// Decides whether an alert should be sent on this scheduler tick (UTC).
/// </summary>
public static class AlertScheduleEvaluator
{
    private static readonly TimeSpan AttemptingCooldown = TimeSpan.FromMinutes(55);

    public static bool IsDue(Alert alert, DateTimeOffset utcNow)
    {
        if (alert.Frequency == FrequencyType.NA)
            return false;

        if (!alert.StartDate.HasValue)
            return false;

        var start = alert.StartDate.Value;
        if (utcNow < start)
            return false;

        if (alert.EmailStatus == EmailStatus.Attempting && alert.LastAttemptedOn.HasValue && utcNow - alert.LastAttemptedOn.Value < AttemptingCooldown)
            return false;

        return alert.Frequency switch
        {
            FrequencyType.OneTime => IsOneTimeDue(alert),
            FrequencyType.Weekly => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 7),
            FrequencyType.EOW => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 14),
            FrequencyType.Monthly => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 30),
            FrequencyType.Quarterly => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 90),
            FrequencyType.BiAnnually => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 180),
            FrequencyType.Annually => IsRecurringPeriodDue(start, utcNow, alert.SentOn, 365),
            _ => false
        };
    }

    private static bool IsOneTimeDue(Alert alert)
    {
        if (alert.SentOn.HasValue || alert.EmailStatus == EmailStatus.Succeeded)
            return false;

        return alert.EmailStatus is EmailStatus.Unsent or EmailStatus.Failed;
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

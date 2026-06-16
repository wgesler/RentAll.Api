using Microsoft.Data.SqlClient;

namespace RentAll.Infrastructure.Configuration;

public static class SqlDeadlockRetry
{
    private const int DeadlockErrorNumber = 1205;
    private const int MaxAttempts = 3;

    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsDeadlock(ex) && attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(50, 150) * attempt));
            }
        }

        throw new InvalidOperationException("Deadlock retry failed unexpectedly.");
    }

    public static Task ExecuteAsync(Func<Task> action)
        => ExecuteAsync(async () =>
        {
            await action();
            return true;
        });

    private static bool IsDeadlock(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException sqlException && sqlException.Number == DeadlockErrorNumber)
                return true;

            if (current.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

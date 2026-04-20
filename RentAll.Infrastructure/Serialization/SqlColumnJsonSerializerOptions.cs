using System.Text.Json;

namespace RentAll.Infrastructure.Serialization;

// SQL column JSON: use DateTimeOffset (or DateTime) for datetimeoffset/datetime from FOR JSON; DateOnly fails on ISO date-time strings.
public static class SqlColumnJsonSerializerOptions
{
    public static JsonSerializerOptions CaseInsensitive { get; } = new() { PropertyNameCaseInsensitive = true };
}

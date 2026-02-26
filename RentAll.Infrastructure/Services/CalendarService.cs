using Microsoft.Extensions.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly byte[] _calendarSecretBytes;

    public CalendarService(IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JwtSettings:SecretKey is required for calendar token signing.");

        _calendarSecretBytes = Encoding.UTF8.GetBytes(secret);
    }

    public string GeneratePropertyCalendarToken(Guid propertyId, Guid organizationId)
    {
        var payload = GetTokenPayload(propertyId, organizationId);
        using var hmac = new HMACSHA256(_calendarSecretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    public bool IsValidPropertyCalendarToken(Guid propertyId, Guid organizationId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var expected = GeneratePropertyCalendarToken(propertyId, organizationId);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(token.Trim());

        return expectedBytes.Length == providedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    public string BuildPropertyCalendar(Guid propertyId, IEnumerable<Reservation> reservations, DateTimeOffset generatedOnUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//RentAll//Property Calendar//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine($"X-WR-CALNAME:Property {propertyId} Availability");

        var stamp = generatedOnUtc.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
        foreach (var reservation in reservations.OrderBy(r => r.ArrivalDate))
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:reservation-{reservation.ReservationId}@rentall");
            sb.AppendLine($"DTSTAMP:{stamp}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{reservation.ArrivalDate.UtcDateTime:yyyyMMdd}");
            sb.AppendLine($"DTEND;VALUE=DATE:{reservation.DepartureDate.UtcDateTime:yyyyMMdd}");
            sb.AppendLine($"SUMMARY:{EscapeIcalText(GetSummary(reservation))}");
            sb.AppendLine("STATUS:CONFIRMED");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString().Replace("\n", "\r\n");
    }

    private static string GetTokenPayload(Guid propertyId, Guid organizationId)
    {
        return $"{organizationId:N}:{propertyId:N}";
    }

    private static string GetSummary(Reservation reservation)
    {
        if (!string.IsNullOrWhiteSpace(reservation.ReservationCode))
            return $"Booked ({reservation.ReservationCode})";

        return "Booked";
    }

    private static string EscapeIcalText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}

using Microsoft.Extensions.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;
using System.Globalization;
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
        var newLine = "\r\n";

        sb.Append("BEGIN:VCALENDAR").Append(newLine);
        sb.Append("VERSION:2.0").Append(newLine);
        sb.Append("PRODID:-//RentAll//Property Calendar//EN").Append(newLine);
        sb.Append("CALSCALE:GREGORIAN").Append(newLine);
        sb.Append("METHOD:PUBLISH").Append(newLine);
        sb.Append($"X-WR-CALNAME:{EscapeIcalText($"Property {propertyId} Availability")}").Append(newLine);

        var stamp = generatedOnUtc.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        foreach (var reservation in reservations.OrderBy(r => r.ArrivalDate))
        {
            var startDate = reservation.ArrivalDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var endDateExclusive = reservation.DepartureDate.AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var summary = EscapeIcalText(GetSummary(reservation));

            sb.Append("BEGIN:VEVENT").Append(newLine);
            sb.Append($"UID:reservation-{reservation.ReservationId}@rentall").Append(newLine);
            sb.Append($"DTSTAMP:{stamp}").Append(newLine);
            sb.Append($"DTSTART;VALUE=DATE:{startDate}").Append(newLine);
            sb.Append($"DTEND;VALUE=DATE:{endDateExclusive}").Append(newLine);
            sb.Append($"SUMMARY:{summary}").Append(newLine);
            sb.Append("STATUS:CONFIRMED").Append(newLine);
            sb.Append("END:VEVENT").Append(newLine);
        }

        sb.Append("END:VCALENDAR").Append(newLine);

        return sb.ToString();
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

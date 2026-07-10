using Microsoft.Extensions.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly byte[] _calendarSecretBytes;
    private readonly HttpClient _httpClient;

    public CalendarService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        var secret = configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JwtSettings:SecretKey is required for calendar token signing.");

        _calendarSecretBytes = Encoding.UTF8.GetBytes(secret);
        _httpClient = httpClientFactory.CreateClient();
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
            var endDate = reservation.DepartureDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var summary = EscapeIcalText(GetSummary(reservation));

            sb.Append("BEGIN:VEVENT").Append(newLine);
            sb.Append($"UID:reservation-{reservation.ReservationId}@rentall").Append(newLine);
            sb.Append($"DTSTAMP:{stamp}").Append(newLine);
            sb.Append($"DTSTART;VALUE=DATE:{startDate}").Append(newLine);
            sb.Append($"DTEND;VALUE=DATE:{endDate}").Append(newLine);
            sb.Append($"SUMMARY:{summary}").Append(newLine);
            sb.Append("STATUS:CONFIRMED").Append(newLine);
            sb.Append("END:VEVENT").Append(newLine);
        }

        sb.Append("END:VCALENDAR").Append(newLine);

        return sb.ToString();
    }

    public async Task<IReadOnlyList<ImportedCalendarEvent>> ImportExternalCalendarAsync(string externalCalendarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalCalendarUrl))
            throw new ArgumentException("External calendar URL is required.", nameof(externalCalendarUrl));

        var normalizedUrl = NormalizeCalendarUrl(externalCalendarUrl);
        using var request = new HttpRequestMessage(HttpMethod.Get, normalizedUrl);
        request.Headers.UserAgent.ParseAdd("RentAll-CalendarImporter/1.0");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var icalText = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseImportedCalendarEvents(icalText);
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

    private static string NormalizeCalendarUrl(string externalCalendarUrl)
    {
        var trimmed = externalCalendarUrl.Trim();
        if (trimmed.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase))
            return "https://" + trimmed["webcal://".Length..];

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            throw new ArgumentException("External calendar URL must be an absolute URL.", nameof(externalCalendarUrl));

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("External calendar URL must use http, https, or webcal scheme.", nameof(externalCalendarUrl));
        }

        return uri.ToString();
    }

    private static IReadOnlyList<ImportedCalendarEvent> ParseImportedCalendarEvents(string icalText)
    {
        var lines = GetUnfoldedLines(icalText);
        var events = new List<ImportedCalendarEvent>();
        Dictionary<string, string>? currentEvent = null;

        foreach (var line in lines)
        {
            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (currentEvent != null && TryMapImportedEvent(currentEvent, out var importedEvent))
                    events.Add(importedEvent);

                currentEvent = null;
                continue;
            }

            if (currentEvent == null)
                continue;

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var namePart = line[..separatorIndex];
            var valuePart = line[(separatorIndex + 1)..];
            currentEvent[namePart] = valuePart;
        }

        return events;
    }

    private static bool TryMapImportedEvent(Dictionary<string, string> rawEvent, out ImportedCalendarEvent importedEvent)
    {
        importedEvent = new ImportedCalendarEvent();

        var dtStartRaw = FindEventProperty(rawEvent, "DTSTART");
        if (string.IsNullOrWhiteSpace(dtStartRaw) || !TryParseIcalDate(dtStartRaw, out var startDate))
            return false;

        var dtEndRaw = FindEventProperty(rawEvent, "DTEND");
        DateOnly endDateExclusive;
        if (!string.IsNullOrWhiteSpace(dtEndRaw) && TryParseIcalDate(dtEndRaw, out var parsedEndDate))
        {
            endDateExclusive = parsedEndDate > startDate ? parsedEndDate : startDate.AddDays(1);
        }
        else
        {
            endDateExclusive = startDate.AddDays(1);
        }

        importedEvent = new ImportedCalendarEvent
        {
            Uid = FindEventProperty(rawEvent, "UID") ?? string.Empty,
            Summary = FindEventProperty(rawEvent, "SUMMARY") ?? string.Empty,
            StartDate = startDate,
            EndDateExclusive = endDateExclusive
        };

        return true;
    }

    private static string? FindEventProperty(Dictionary<string, string> rawEvent, string propertyName)
    {
        foreach (var keyValue in rawEvent)
        {
            var key = keyValue.Key;
            var normalized = key.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            if (normalized.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return keyValue.Value;
        }

        return null;
    }

    private static bool TryParseIcalDate(string rawValue, out DateOnly value)
    {
        value = default;
        var trimmed = rawValue.Trim();
        if (trimmed.Length < 8)
            return false;

        var datePortion = trimmed[..8];
        return DateOnly.TryParseExact(datePortion, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    private static IReadOnlyList<string> GetUnfoldedLines(string icalText)
    {
        var normalized = (icalText ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var physicalLines = normalized.Split('\n');
        var unfoldedLines = new List<string>();

        foreach (var line in physicalLines)
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && unfoldedLines.Count > 0)
            {
                unfoldedLines[^1] += line[1..];
                continue;
            }

            if (!string.IsNullOrEmpty(line))
                unfoldedLines.Add(line);
        }

        return unfoldedLines;
    }
}

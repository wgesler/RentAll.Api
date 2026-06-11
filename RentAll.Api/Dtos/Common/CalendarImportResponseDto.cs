using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Common;

public class CalendarImportResponseDto
{
    public string ExternalCalendarUrl { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public IReadOnlyList<CalendarImportEventDto> Events { get; set; } = Array.Empty<CalendarImportEventDto>();

    public static CalendarImportResponseDto FromImportedEvents(string externalCalendarUrl, IReadOnlyList<ImportedCalendarEvent> importedEvents)
    {
        return new CalendarImportResponseDto
        {
            ExternalCalendarUrl = externalCalendarUrl,
            EventCount = importedEvents.Count,
            Events = importedEvents
                .OrderBy(e => e.StartDate)
                .Select(e => new CalendarImportEventDto
                {
                    Uid = e.Uid,
                    Summary = e.Summary,
                    ArrivalDate = e.StartDate,
                    DepartureDate = ResolveDepartureDate(e.StartDate, e.EndDateExclusive)
                })
                .ToList()
        };
    }

    private static DateOnly ResolveDepartureDate(DateOnly startDate, DateOnly endDateExclusive)
    {
        // ICS DTEND is exclusive for all-day events; UI reservation style expects a concrete departure date.
        var candidate = endDateExclusive.AddDays(-1);
        return candidate < startDate ? startDate : candidate;
    }
}

public class CalendarImportEventDto
{
    public string Uid { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
}

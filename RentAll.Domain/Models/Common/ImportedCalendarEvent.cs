namespace RentAll.Domain.Models.Common;

public class ImportedCalendarEvent
{
    public string Uid { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDateExclusive { get; set; }
}

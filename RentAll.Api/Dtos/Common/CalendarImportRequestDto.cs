namespace RentAll.Api.Dtos.Common;

public class CalendarImportRequestDto
{
    public string ExternalCalendarUrl { get; set; } = string.Empty;
    public string? PropertyCode { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(ExternalCalendarUrl))
            return (false, "ExternalCalendarUrl is required");

        return (true, null);
    }
}

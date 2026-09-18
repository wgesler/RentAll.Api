namespace RentAll.Api.Dtos.Properties.Properties;

public static class PropertyICalDto
{
    public static List<string> Normalize(IEnumerable<string>? urls)
    {
        return (urls ?? [])
            .Select(url => (url ?? string.Empty).Trim())
            .Where(url => url.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> MergeCalendarInputs(IEnumerable<string>? calendars, string? legacyUrl)
    {
        var items = Normalize(calendars);
        var url = (legacyUrl ?? string.Empty).Trim();
        if (url.Length > 0 && items.All(item => !string.Equals(item, url, StringComparison.OrdinalIgnoreCase)))
            items.Add(url);

        return items;
    }
}

namespace RentAll.Api.Dtos.Accounting.ClosedDates;

public class GetClosedDatesByCriteriaDto
{
    public List<int> OfficeIds { get; set; } = new();
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? PostingStatusId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        var allowedOfficeIds = currentOffices
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.Parse(id))
            .ToHashSet();

        var officeIds = (OfficeIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
        if (officeIds.Count == 0)
            return (false, "At least one office ID is required");

        if (officeIds.Any(id => !allowedOfficeIds.Contains(id)))
            return (false, "Unauthorized");

        if (StartDate != null && EndDate != null && StartDate > EndDate)
            return (false, "StartDate must be on or before EndDate");

        if (PostingStatusId != null && !Enum.IsDefined(typeof(Domain.Enums.PostingStatus), PostingStatusId.Value))
            return (false, "Invalid PostingStatusId");

        return (true, null);
    }

    public string ToOfficeIdsCsv()
        => string.Join(',', (OfficeIds ?? new List<int>()).Where(id => id > 0).Distinct());
}

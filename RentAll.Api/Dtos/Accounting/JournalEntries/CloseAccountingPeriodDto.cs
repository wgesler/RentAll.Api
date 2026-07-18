namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class CloseAccountingPeriodDto
{
    public int OfficeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PostingStatusId { get; set; }
    public List<Guid> JournalEntryIds { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (StartDate == default)
            return (false, "StartDate is required");

        if (EndDate == default)
            return (false, "EndDate is required");

        if (StartDate > EndDate)
            return (false, "StartDate must be on or before EndDate");

        if (PostingStatusId != (int)PostingStatus.SoftClosed && PostingStatusId != (int)PostingStatus.HardClosed)
            return (false, "PostingStatusId must be SoftClosed or HardClosed");

        if (JournalEntryIds != null && JournalEntryIds.Any(id => id == Guid.Empty))
            return (false, "Invalid journal entry ID");

        return (true, null);
    }

    public PostingStatus ToCloseStatus()
        => (PostingStatus)PostingStatusId;
}

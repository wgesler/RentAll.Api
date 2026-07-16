using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.ClosedDates;

public class UpdateClosedDateDto
{
    public int ClosedDateId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PostingStatusId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (ClosedDateId <= 0)
            return (false, "ClosedDateId is required");

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

        if (!Enum.IsDefined(typeof(PostingStatus), PostingStatusId))
            return (false, "Invalid PostingStatusId");

        return (true, null);
    }

    public ClosedDate ToModel(Guid organizationId)
    {
        return new ClosedDate
        {
            ClosedDateId = ClosedDateId,
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            StartDate = StartDate,
            EndDate = EndDate,
            PostingStatusId = (PostingStatus)PostingStatusId
        };
    }
}

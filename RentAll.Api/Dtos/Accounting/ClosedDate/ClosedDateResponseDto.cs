using ClosedDateModel = RentAll.Domain.Models.ClosedDate;

namespace RentAll.Api.Dtos.Accounting.ClosedDate;

public class ClosedDateResponseDto
{
    public int ClosedDateId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PostingStatusId { get; set; }

    public ClosedDateResponseDto(ClosedDateModel closedDate)
    {
        ClosedDateId = closedDate.ClosedDateId;
        OrganizationId = closedDate.OrganizationId;
        OfficeId = closedDate.OfficeId;
        StartDate = closedDate.StartDate;
        EndDate = closedDate.EndDate;
        PostingStatusId = (int)closedDate.PostingStatusId;
    }
}

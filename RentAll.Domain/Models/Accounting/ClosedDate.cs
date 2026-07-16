using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ClosedDate
{
    public int ClosedDateId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public PostingStatus PostingStatusId { get; set; }
}

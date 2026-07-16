namespace RentAll.Infrastructure.Entities.Accounting;

public class ClosedDateEntity
{
    public int ClosedDateId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int PostingStatusId { get; set; }
}

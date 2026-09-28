using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Billed
{
    public int BilledId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly InvoiceStart { get; set; }
    public BillingType BillingType { get; set; }
    public int TotalNumberOfDays { get; set; }
    public int DaysSinceStart { get; set; }
    public int DaysStayed { get; set; }
    public int DaysBilled { get; set; }
    public List<string> RentalFeeLines { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

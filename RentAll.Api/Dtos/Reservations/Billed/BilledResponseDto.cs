using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Reservations.Billed;

public class BilledResponseDto
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
    public IReadOnlyList<string> RentalFeeLines { get; set; } = Array.Empty<string>();
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public BilledResponseDto(RentAll.Domain.Models.Billed billed)
    {
        BilledId = billed.BilledId;
        OrganizationId = billed.OrganizationId;
        OfficeId = billed.OfficeId;
        ReservationId = billed.ReservationId;
        ReservationCode = billed.ReservationCode;
        StartDate = billed.StartDate;
        EndDate = billed.EndDate;
        InvoiceStart = billed.InvoiceStart;
        BillingType = billed.BillingType;
        TotalNumberOfDays = billed.TotalNumberOfDays;
        DaysSinceStart = billed.DaysSinceStart;
        DaysStayed = billed.DaysStayed;
        DaysBilled = billed.DaysBilled;
        RentalFeeLines = billed.RentalFeeLines ?? [];
        CreatedOn = billed.CreatedOn;
        CreatedBy = billed.CreatedBy;
        ModifiedOn = billed.ModifiedOn;
        ModifiedBy = billed.ModifiedBy;
    }
}

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
    public DateOnly MonthStart { get; set; }
    public DateOnly MonthEnd { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int DaysStayed { get; set; }
    public int DaysBilled { get; set; }
    public IReadOnlyList<string> RentalFeeLines { get; set; } = Array.Empty<string>();
    public bool Ignore { get; set; }
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
        MonthStart = billed.MonthStart;
        MonthEnd = billed.MonthEnd;
        PeriodStart = billed.PeriodStart;
        PeriodEnd = billed.PeriodEnd;
        DaysStayed = billed.DaysStayed;
        DaysBilled = billed.DaysBilled;
        RentalFeeLines = billed.RentalFeeLines ?? [];
        Ignore = billed.Ignore;
        CreatedOn = billed.CreatedOn;
        CreatedBy = billed.CreatedBy;
        ModifiedOn = billed.ModifiedOn;
        ModifiedBy = billed.ModifiedBy;
    }
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations.Billed;

public class CreateBilledDto
{
    public Guid ReservationId { get; set; }
    public int OfficeId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly InvoiceStart { get; set; }
    public BillingType BillingType { get; set; }
    public int TotalNumberOfDays { get; set; }
    public int DaysSinceStart { get; set; }
    public int DaysStayed { get; set; }
    public int DaysBilled { get; set; }
    public List<string>? RentalFeeLines { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");
        if (OfficeId <= 0)
            return (false, "OfficeId is required");
        if (EndDate < StartDate)
            return (false, "EndDate must be on or after StartDate");
        if (TotalNumberOfDays < 0 || DaysSinceStart < 0 || DaysStayed < 0 || DaysBilled < 0)
            return (false, "Day counts cannot be negative");
        return (true, null);
    }

    public RentAll.Domain.Models.Billed ToModel(Guid organizationId, Guid currentUser)
        => new()
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            ReservationId = ReservationId,
            ReservationCode = (ReservationCode ?? string.Empty).Trim(),
            StartDate = StartDate,
            EndDate = EndDate,
            InvoiceStart = InvoiceStart == default ? BilledMatchupInvoiceStart.InvoiceStart : InvoiceStart,
            BillingType = BillingType,
            TotalNumberOfDays = TotalNumberOfDays,
            DaysSinceStart = DaysSinceStart,
            DaysStayed = DaysStayed,
            DaysBilled = DaysBilled,
            RentalFeeLines = (RentalFeeLines ?? [])
                .Select(line => (line ?? string.Empty).Trim())
                .Where(line => line.Length > 0)
                .ToList(),
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
}

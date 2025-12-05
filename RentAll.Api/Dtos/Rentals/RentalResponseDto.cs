using RentAll.Domain.Models.Rentals;

namespace RentAll.Api.Dtos.Rentals;

public class RentalResponseDto
{
    public Guid RentalId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContactId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public RentalResponseDto(Rental rental)
    {
        RentalId = rental.RentalId;
        PropertyId = rental.PropertyId;
        ContactId = rental.ContactId;
        StartDate = rental.StartDate;
        EndDate = rental.EndDate;
        DailyRate = rental.DailyRate;
        IsActive = rental.IsActive;
        CreatedOn = rental.CreatedOn;
        CreatedBy = rental.CreatedBy;
        ModifiedOn = rental.ModifiedOn;
        ModifiedBy = rental.ModifiedBy;
    }
}
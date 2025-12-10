using RentAll.Domain.Models.Rentals;

namespace RentAll.Api.Dtos.Rentals;

public class CreateRentalDto
{
    public Guid PropertyId { get; set; }
    public Guid ContactId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DailyRate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "Property ID is required");

        if (ContactId == Guid.Empty)
            return (false, "Contact ID is required");

        if (StartDate >= EndDate)
            return (false, "End Date must be after Start Date");

        if (DailyRate <= 0)
            return (false, "Daily Rate must be greater than zero");

        return (true, null);
    }

    public Rental ToModel(Guid currentUser)
    {
        return new Rental
        {
            RentalId = Guid.NewGuid(),
            PropertyId = PropertyId,
            ContactId = ContactId,
            StartDate = StartDate,
            EndDate = EndDate,
            DailyRate = DailyRate,
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}


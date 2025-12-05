using RentAll.Domain.Models.Rentals;

namespace RentAll.Api.Dtos.Rentals;

public class UpdateRentalDto
{
	public Guid RentalId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
	public decimal DailyRate { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
	{
		if (id == Guid.Empty)
			return (false, "Rental ID is required");

		if (RentalId != id)
			return (false, "Rental ID mismatch");

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

	public Rental ToModel(Rental existingRental, Guid currentUser)
	{
		return new Rental
		{
			RentalId = RentalId,
			PropertyId = PropertyId,
			ContactId = ContactId,
			StartDate = StartDate,
			EndDate = EndDate,
			DailyRate = DailyRate,
			IsActive = existingRental.IsActive,
			CreatedOn = existingRental.CreatedOn,
			CreatedBy = existingRental.CreatedBy,
			ModifiedBy = currentUser
		};
	}
}


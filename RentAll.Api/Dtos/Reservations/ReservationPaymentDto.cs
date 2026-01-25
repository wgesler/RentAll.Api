using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations;

public class ReservationPaymentDto
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public decimal Payment { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (ReservationId == Guid.Empty)
			return (false, "ReservationId is required");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (Payment <= 0)
			return (false, "No payment submitted");

		return (true, null);
	}
}



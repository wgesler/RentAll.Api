namespace RentAll.Api.Dtos.Reservations;

public class ReservationPaymentDto
{
	public Guid ReservationId { get; set; }
	public int CostCodeId { get; set; }
	public string Description { get; set; } = string.Empty;
	public decimal Amount { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (ReservationId == Guid.Empty)
			return (false, "ReservationId is required");

		if (CostCodeId < 0)
			return (false, "CostCodeId is required");

		if (Amount <= 0)
			return (false, "No payment submitted");

		return (true, null);
	}
}



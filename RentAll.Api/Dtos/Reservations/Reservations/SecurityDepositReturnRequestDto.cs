using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class SecurityDepositReturnRequestDto
{
    public Guid ReservationId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public int ChartOfAccountId { get; set; }
    public int PaymentTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (!Enum.IsDefined(typeof(PaymentType), PaymentTypeId))
            return (false, $"Invalid PaymentType value: {PaymentTypeId}");

        if (Amount == 0)
            return (false, "No payment submitted");

        return (true, null);
    }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationCodeResponseDto
{
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;

    public ReservationCodeResponseDto(ReservationCode reservationCode)
    {
        ReservationId = reservationCode.ReservationId;
        ReservationCode = reservationCode.ReservationCode;
        PropertyId = reservationCode.PropertyId;
        PropertyCode = reservationCode.PropertyCode;
        OfficeId = reservationCode.OfficeId;
        OfficeName = reservationCode.OfficeName;
    }
}

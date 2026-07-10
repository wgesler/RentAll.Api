namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationCodeResponseDto
{
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int ReservationTypeId { get; set; }
    public bool IsActive { get; set; }

    public ReservationCodeResponseDto(ReservationCodes reservationCode)
    {
        ReservationId = reservationCode.ReservationId;
        ReservationCode = reservationCode.ReservationCode;
        PropertyId = reservationCode.PropertyId;
        PropertyCode = reservationCode.PropertyCode;
        OfficeId = reservationCode.OfficeId;
        OfficeName = reservationCode.OfficeName;
        ContactId = reservationCode.ContactId;
        ContactName = reservationCode.ContactName;
        CompanyId = reservationCode.CompanyId;
        CompanyName = reservationCode.CompanyName;
        TenantName = reservationCode.TenantName;
        ReservationTypeId = reservationCode.ReservationTypeId;
        IsActive = reservationCode.IsActive;
    }
}

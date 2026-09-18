namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ExternalReservationBatchItemResultDto
{
    public int Index { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public string? ReservationCode { get; set; }
    public bool Success { get; set; }
    public bool Updated { get; set; }
    public string? ErrorMessage { get; set; }
    public ReservationResponseDto? Reservation { get; set; }
}

public class ExternalReservationBatchResponseDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ExternalReservationBatchItemResultDto> Results { get; set; } = [];
}

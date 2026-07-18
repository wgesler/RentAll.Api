namespace RentAll.Api.Dtos.Reservations.Reservations;

public class UnreturnedSecurityDepositsResponseDto
{
    public List<ReservationDepartureResponseDto> Rows { get; set; } = [];
    public decimal TotalDepositsOwed { get; set; }
    public decimal EscrowBalance { get; set; }
    public decimal Discrepancy { get; set; }
    public string EscrowAccountLabel { get; set; } = string.Empty;

    public UnreturnedSecurityDepositsResponseDto(UnreturnedSecurityDepositsResult result)
    {
        Rows = (result.Rows ?? []).Select(row => new ReservationDepartureResponseDto(row)).ToList();
        TotalDepositsOwed = result.TotalDepositsOwed;
        EscrowBalance = result.EscrowBalance;
        Discrepancy = result.Discrepancy;
        EscrowAccountLabel = result.EscrowAccountLabel;
    }
}

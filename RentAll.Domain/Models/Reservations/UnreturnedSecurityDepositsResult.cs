namespace RentAll.Domain.Models;

public class UnreturnedSecurityDepositsResult
{
    public List<ReservationDeparture> Rows { get; set; } = [];
    public decimal TotalDepositsOwed { get; set; }
    public decimal EscrowBalance { get; set; }
    public decimal Discrepancy { get; set; }
    public string EscrowAccountLabel { get; set; } = string.Empty;
}

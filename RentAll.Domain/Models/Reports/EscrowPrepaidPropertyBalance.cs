namespace RentAll.Domain.Models;

public class EscrowPrepaidPropertyBalance
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Balance { get; set; }
}

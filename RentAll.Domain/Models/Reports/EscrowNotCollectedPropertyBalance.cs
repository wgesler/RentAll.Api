namespace RentAll.Domain.Models;

public class EscrowNotCollectedPropertyBalance
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal NotCollectedAmount { get; set; }
}

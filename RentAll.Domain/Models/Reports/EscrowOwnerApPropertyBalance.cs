namespace RentAll.Domain.Models;

public class EscrowOwnerApPropertyBalance
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal ApBalance { get; set; }
}

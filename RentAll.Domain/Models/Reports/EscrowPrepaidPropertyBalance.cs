namespace RentAll.Domain.Models;

public class EscrowPrepaidPropertyBalance
{
    public Guid JournalEntryLineId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Balance { get; set; }
    public decimal ExpectedIncome { get; set; }
    public decimal OwnerRent { get; set; }
}

namespace RentAll.Infrastructure.Entities.Accounting;

public class EscrowPrepaidPropertyBalanceEntity
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public decimal Prepaids { get; set; }
}

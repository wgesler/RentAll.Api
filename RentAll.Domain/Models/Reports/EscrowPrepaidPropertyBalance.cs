namespace RentAll.Domain.Models;

public class EscrowPrepaidPropertyBalance
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public decimal Prepaids { get; set; }
}

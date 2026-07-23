namespace RentAll.Domain.Models;

public class EscrowOfficeBalance
{
    public int OfficeId { get; set; }
    public int AccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

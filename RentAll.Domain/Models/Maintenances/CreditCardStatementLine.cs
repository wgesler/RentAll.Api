namespace RentAll.Domain.Models.Maintenances;

public class CreditCardStatementLine
{
    public DateOnly? ChargeDate { get; set; }
    public decimal? Amount { get; set; }
    public string? VendorName { get; set; }
    public string? Description { get; set; }
    public string? CardLastFour { get; set; }
    public int? CardTypeId { get; set; }
}

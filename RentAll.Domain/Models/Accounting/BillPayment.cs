namespace RentAll.Domain.Models;

public class BillPayment
{
    public List<Receipt> Bills { get; set; } = new List<Receipt>();
}

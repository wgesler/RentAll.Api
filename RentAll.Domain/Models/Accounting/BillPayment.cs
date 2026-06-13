namespace RentAll.Domain.Models;

public class BillPayment
{
    public List<Receipt> Bills { get; set; } = new List<Receipt>();
    public List<BillPaymentApplication> PaymentApplications { get; set; } = new List<BillPaymentApplication>();
}

public class BillPaymentApplication
{
    public Receipt Bill { get; set; } = null!;
    public decimal AmountApplied { get; set; }
    public DateOnly PaymentDate { get; set; }
    public int CostCodeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentSequence { get; set; }
}

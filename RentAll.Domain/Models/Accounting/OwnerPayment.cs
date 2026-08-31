using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class OwnerPayments
{
    public DateOnly PaymentDate { get; set; }
    public List<OwnerPayment> Payments { get; set; } = new List<OwnerPayment>();
}

public class OwnerPayment
{
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
}

public class OwnerPaymentBatch
{
    public List<OwnerPaymentApplication> PaymentApplications { get; set; } = new List<OwnerPaymentApplication>();
}

public class OwnerPaymentApplication
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public decimal AmountApplied { get; set; }
    public DateOnly PaymentDate { get; set; }
    public int ChartOfAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public Guid? PaymentId { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
}

using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class OwnerPayment
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
}

public readonly record struct OwnerPaymentLine(int OfficeId, Guid OwnerId, Guid PropertyId, decimal Amount);

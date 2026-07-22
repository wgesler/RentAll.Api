namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Models;

public class ApplyInvoicePaymentDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public int CostCodeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? PaymentTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> Invoices { get; set; } = new();
    public List<PaymentInvoiceAllocationDto> Allocations { get; set; } = new();

    public bool UsesExplicitAllocations => Allocations.Count > 0;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (CostCodeId <= 0)
            return (false, "CostCodeId is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (Amount == 0)
            return (false, "No payment submitted");

        if (UsesExplicitAllocations)
        {
            foreach (var allocation in Allocations)
            {
                var (isValid, errorMessage) = allocation.IsValid();
                if (!isValid)
                    return (false, errorMessage);
            }

            var allocationTotal = Allocations.Sum(allocation => allocation.Amount);
            if (allocationTotal != Amount)
                return (false, "Allocation total must equal the payment amount");

            return (true, null);
        }

        if (Invoices.Count <= 0)
            return (false, "No invoices submitted for payment");

        return (true, null);
    }

    public Payment ToModel(Guid currentUser)
    {
        return new Payment
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PaymentDate = PaymentDate,
            Amount = Amount,
            CostCodeId = CostCodeId,
            Description = Description,
            PaymentTypeId = PaymentTypeId is >= 0 ? PaymentTypeId : null,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }

    public IReadOnlyList<Guid> ResolveInvoiceIdsForPostingCheck()
    {
        if (UsesExplicitAllocations)
            return Allocations.Select(allocation => allocation.InvoiceId).Distinct().ToList();

        return Invoices;
    }
}

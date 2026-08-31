namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

public class CreatePaymentWithOwnerAllocationsDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? PaymentTypeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PaymentOwnerAllocationDto> Allocations { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (Allocations == null || Allocations.Count == 0)
            return (false, "At least one owner allocation is required");

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

    public Payment ToModel(Guid currentUser)
    {
        return new Payment
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PaymentDate = PaymentDate,
            Amount = Amount,
            Description = Description,
            PaymentKindId = (int)PaymentKind.Owner,
            PaymentTypeId = PaymentTypeId is >= 0 ? PaymentTypeId : null,
            ChartOfAccountId = ChartOfAccountId,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

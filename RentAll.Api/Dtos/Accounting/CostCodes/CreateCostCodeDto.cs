using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.CostCodes;

public class CreateCostCodeDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string CostCode { get; set; } = string.Empty;
    public int TransactionTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (string.IsNullOrWhiteSpace(CostCode))
            return (false, "CostCode is required");

        if (!Enum.IsDefined(typeof(TransactionType), TransactionTypeId))
            return (false, "Invalid TransactionTypeId");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        return (true, null);
    }

    public CostCode ToModel()
    {
        return new CostCode
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            Code = CostCode,
            TransactionType = (TransactionType)TransactionTypeId,
            Description = Description,
            IsActive = IsActive
        };
    }
}

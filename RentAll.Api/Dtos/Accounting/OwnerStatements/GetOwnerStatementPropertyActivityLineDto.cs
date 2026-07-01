using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class GetOwnerStatementPropertyActivityLineDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid PropertyId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        return (true, null);
    }

    public OwnerStatementPropertyActivityGetCriteria ToCriteria(Guid organizationId)
    {
        return new OwnerStatementPropertyActivityGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = ResolvedOfficeIds,
            PropertyId = PropertyId,
            StartDate = StartDate,
            EndDate = EndDate
        };
    }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class GetOwnerStatementJournalEntryLineDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid OwnerId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (OwnerId == Guid.Empty)
            return (false, "OwnerId is required");

        if (!string.IsNullOrWhiteSpace(Metric))
        {
            var metric = Metric.Trim().ToLowerInvariant();
            if (metric != "expected" && metric != "prepaid" && metric != "outstanding" && metric != "income" && metric != "expenses" && metric != "balance")
                return (false, "Metric must be one of: expected, prePaid, outstanding, income, expenses, balance");
        }

        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        return (true, null);
    }

    public OwnerStatementJournalEntryLineGetCriteria ToCriteria(Guid organizationId)
    {
        return new OwnerStatementJournalEntryLineGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = ResolvedOfficeIds,
            OwnerId = OwnerId,
            PropertyId = PropertyId,
            Metric = (Metric ?? string.Empty).Trim(),
            StartDate = StartDate,
            EndDate = EndDate
        };
    }
}

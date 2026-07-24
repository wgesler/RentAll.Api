using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class GetEscrowReportJournalEntryLineDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid? PropertyId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public bool IncludeUnposted { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (!EndDate.HasValue)
            return (false, "EndDate is required");

        if (!string.IsNullOrWhiteSpace(Metric))
        {
            var metric = Metric.Trim().ToLowerInvariant();
            if (metric != "arbalance"
                && metric != "prepaids"
                && metric != "notcollected"
                && metric != "total"
                && metric != "e2"
                && metric != "escrowbankbalance"
                && metric != "transfer")
            {
                return (false, "Metric must be one of: arBalance, prepaids, notCollected, total, e2, escrowBankBalance, transfer");
            }
        }

        return (true, null);
    }

    public EscrowReportJournalEntryDrillDownCriteria ToCriteria(Guid organizationId)
    {
        return new EscrowReportJournalEntryDrillDownCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = ResolvedOfficeIds,
            PropertyId = PropertyId,
            Metric = (Metric ?? string.Empty).Trim(),
            EndDate = EndDate,
            IncludeUnposted = IncludeUnposted
        };
    }
}

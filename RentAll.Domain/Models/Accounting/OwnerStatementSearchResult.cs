namespace RentAll.Domain.Models;

public class OwnerStatementSearchResult
{
    public List<OwnerStatementSummary> Summaries { get; set; } = new List<OwnerStatementSummary>();
    public List<OwnerStatementPropertyActivityLine> PropertyActivityLines { get; set; } = new List<OwnerStatementPropertyActivityLine>();
}

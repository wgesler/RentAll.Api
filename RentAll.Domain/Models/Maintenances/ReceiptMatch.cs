namespace RentAll.Domain.Models;

public class ReceiptMatch
{
    public Guid ReceiptMatchId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public Guid? MatchedId { get; set; }
    public string? MatchedName { get; set; }
    public bool IsActive { get; set; }
}

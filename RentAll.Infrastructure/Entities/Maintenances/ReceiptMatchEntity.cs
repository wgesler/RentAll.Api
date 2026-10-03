namespace RentAll.Infrastructure.Entities.Maintenances;

public class ReceiptMatchEntity
{
    public Guid ReceiptMatchId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public Guid? MatchedId { get; set; }
    public string? MatchedName { get; set; }
    public bool IsActive { get; set; }

    public RentAll.Domain.Models.ReceiptMatch ToModel()
    {
        return new RentAll.Domain.Models.ReceiptMatch
        {
            ReceiptMatchId = ReceiptMatchId,
            OrganizationId = OrganizationId,
            SourceName = SourceName,
            MatchedId = MatchedId,
            MatchedName = MatchedName,
            IsActive = IsActive
        };
    }
}

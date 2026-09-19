namespace RentAll.Domain.Models.Common;

public class CodeSequence
{
    public Guid OrganizationId { get; set; }
    public int EntityTypeId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int NextNumber { get; set; }
}

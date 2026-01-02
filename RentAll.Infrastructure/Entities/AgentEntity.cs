namespace RentAll.Infrastructure.Entities;

public class AgentEntity
{
    public Guid AgentId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
	public string AgentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}


using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Agents;

public class AgentResponseDto
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


	public AgentResponseDto(Agent agent)
    {
        AgentId = agent.AgentId;
        OrganizationId = agent.OrganizationId;
        OfficeId = agent.OfficeId;
        AgentCode = agent.AgentCode;
		Name = agent.Name;
        IsActive = agent.IsActive;
		CreatedOn = agent.CreatedOn;
		CreatedBy = agent.CreatedBy;
		ModifiedOn = agent.ModifiedOn;
		ModifiedBy = agent.ModifiedBy;
	}
}


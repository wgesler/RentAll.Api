using RentAll.Domain.Models.Agents;

namespace RentAll.Api.Dtos.Agents;

public class AgentResponseDto
{
    public Guid AgentId { get; set; }
    public string AgentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public AgentResponseDto(Agent agent)
    {
        AgentId = agent.AgentId;
        AgentCode = agent.AgentCode;
        Description = agent.Description;
        IsActive = agent.IsActive;
    }
}


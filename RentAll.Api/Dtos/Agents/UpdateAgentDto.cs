using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Agents;

public class UpdateAgentDto
{
	public Guid AgentId { get; set; }
	public Guid OrganizationId { get; set; }
    public string AgentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "Agent ID is required");

        if (AgentId != id)
            return (false, "Agent ID mismatch");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(AgentCode))
            return (false, "Agent Code is required");

        if (AgentCode.Length > 10)
            return (false, "Agent Code must be 10 characters or less");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (Description.Length > 50)
            return (false, "Description must be 50 characters or less");

        return (true, null);
    }

    public Agent ToModel(Guid currentUser)
    {
        return new Agent
        {
            AgentId = AgentId,
            OrganizationId = OrganizationId,
            AgentCode = AgentCode,
            Description = Description,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}


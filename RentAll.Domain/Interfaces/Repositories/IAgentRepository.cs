using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAgentRepository
{
    // Creates
    Task<Agent> CreateAsync(Agent agent);

	// Selects
	Task<IEnumerable<Agent>> GetAllAsync(Guid OrganizationId);
	Task<Agent?> GetByIdAsync(Guid agentId, Guid OrganizationId);
    Task<Agent?> GetByAgentCodeAsync(string agentCode, Guid OrganizationId);
    Task<bool> ExistsByAgentCodeAsync(string agentCode, Guid OrganizationId);

    // Updates
    Task<Agent> UpdateByIdAsync(Agent agent);

    // Deletes
    Task DeleteByIdAsync(Guid agentId);
}


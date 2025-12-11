using RentAll.Domain.Models.Agents;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAgentRepository
{
    // Creates
    Task<Agent> CreateAsync(Agent agent);

    // Selects
    Task<Agent?> GetByIdAsync(Guid agentId);
    Task<Agent?> GetByAgentCodeAsync(string agentCode);
    Task<IEnumerable<Agent>> GetAllAsync();
    Task<bool> ExistsByAgentCodeAsync(string agentCode);

    // Updates
    Task<Agent> UpdateByIdAsync(Agent agent);

    // Deletes
    Task DeleteByIdAsync(Guid agentId);
}


using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Agents;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Agents
{
    public partial class AgentRepository : IAgentRepository
    {
        public async Task<Agent> CreateAsync(Agent agent)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_Add", new
            {
                AgentCode = agent.AgentCode,
                Description = agent.Description,
                IsActive = agent.IsActive,
                CreatedBy = agent.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Agent not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}




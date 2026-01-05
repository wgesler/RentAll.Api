using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Agents
{
    public partial class AgentRepository : IAgentRepository
    {
        public async Task<Agent> UpdateByIdAsync(Agent agent)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_UpdateById", new
            {
				AgentId = agent.AgentId,
				OrganizationId = agent.OrganizationId,
                OfficeId = agent.OfficeId,
                AgentCode = agent.AgentCode,
				Name = agent.Name,
                IsActive = agent.IsActive,
                ModifiedBy = agent.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Agent not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}





using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Agents;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Agents
{
    public partial class AgentRepository : IAgentRepository
    {
        public async Task<IEnumerable<Agent>> GetAllAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_GetAll", null);

            if (res == null || !res.Any())
                return Enumerable.Empty<Agent>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Agent?> GetByIdAsync(Guid agentId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_GetById", new
            {
                AgentId = agentId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<Agent?> GetByAgentCodeAsync(string agentCode)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_GetByCode", new
            {
                AgentCode = agentCode
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<bool> ExistsByAgentCodeAsync(string agentCode)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryScalarAsync<int>("dbo.Agent_ExistsByCode", new
            {
                AgentCode = agentCode
            });

            return result == 1;
        }
    }
}


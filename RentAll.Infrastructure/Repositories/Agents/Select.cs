using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Agents
{
    public partial class AgentRepository : IAgentRepository
    {
        public async Task<IEnumerable<Agent>> GetAllAsync(Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
                return Enumerable.Empty<Agent>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Agent?> GetByIdAsync(Guid agentId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_GetById", new
            {
                AgentId = agentId,
				OrganizationId = organizationId
			});

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<Agent?> GetByAgentCodeAsync(string agentCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AgentEntity>("dbo.Agent_GetByCode", new
            {
                AgentCode = agentCode,
				OrganizationId = organizationId
			});

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<bool> ExistsByAgentCodeAsync(string agentCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryScalarAsync<int>("dbo.Agent_ExistsByCode", new
            {
                AgentCode = agentCode,
				OrganizationId = organizationId
			});

            return result == 1;
        }
    }
}


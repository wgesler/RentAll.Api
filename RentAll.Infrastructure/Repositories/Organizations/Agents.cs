using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Create
    public async Task<Agent> CreateAgentAsync(Agent agent)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AgentEntity>("Organization.Agent_Add", new
        {
            OrganizationId = agent.OrganizationId,
            OfficeId = agent.OfficeId,
            AgentCode = agent.AgentCode,
            Name = agent.Name,
            IsActive = agent.IsActive,
            CreatedBy = agent.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Agent not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<IEnumerable<Agent>> GetAllAgentsAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AgentEntity>("Organization.Agent_GetAll", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Agent>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Agent?> GetAgentByIdAsync(Guid agentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AgentEntity>("Organization.Agent_GetById", new
        {
            AgentId = agentId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Agent?> GetAgentByCodeAsync(string agentCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AgentEntity>("Organization.Agent_GetByCode", new
        {
            AgentCode = agentCode,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsAgentByCodeAsync(string agentCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Organization.Agent_ExistsByCode", new
        {
            AgentCode = agentCode,
            OrganizationId = organizationId
        });

        return result == 1;
    }
    #endregion

    #region Update
    public async Task<Agent> UpdateAgentByIdAsync(Agent agent)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AgentEntity>("Organization.Agent_UpdateById", new
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
    #endregion

    #region Delete
    public async Task DeleteAgentByIdAsync(Guid agentId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Agent_DeleteById", new
        {
            AgentId = agentId
        });
    }
    #endregion
}

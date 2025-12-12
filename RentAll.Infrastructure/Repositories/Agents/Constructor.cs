using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Agents;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Agents
{
    public partial class AgentRepository : IAgentRepository
    {
        private readonly string _dbConnectionString;

        public AgentRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private Agent ConvertEntityToModel(AgentEntity e)
        {
            var response = new Agent()
            {
                AgentId = e.AgentId,
                AgentCode = e.AgentCode,
                Description = e.Description,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }
    }
}



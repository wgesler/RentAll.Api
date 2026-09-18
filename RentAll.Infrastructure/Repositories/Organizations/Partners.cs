using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Organizations;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    public async Task<IEnumerable<OrganizationPartnerOption>> GetOrganizationsWithPartnerFeatureAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<OrganizationPartnerOptionEntity>("Organization.Organization_GetAllWithPartnerFeature", new
        {
            OrganizationId = organizationId
        });

        return (rows ?? []).Select(row => new OrganizationPartnerOption
        {
            OrganizationId = row.OrganizationId,
            OrganizationCode = row.OrganizationCode,
            Name = row.Name
        });
    }

    public async Task<IEnumerable<Guid>> GetPartnersInByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<PartnerOrganizationIdEntity>("Organization.PartnersIn_GetByOrganizationId", new
        {
            OrganizationId = organizationId
        });

        return (rows ?? []).Select(row => row.PartnerOrganizationId);
    }

    public async Task<IEnumerable<Guid>> GetPartnersOutByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<PartnerOrganizationIdEntity>("Organization.PartnersOut_GetByOrganizationId", new
        {
            OrganizationId = organizationId
        });

        return (rows ?? []).Select(row => row.PartnerOrganizationId);
    }

    public async Task AddPartnerInAsync(Guid organizationId, Guid partnerOrganizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.PartnersIn_Add", new
        {
            OrganizationId = organizationId,
            PartnerOrganizationId = partnerOrganizationId
        });
    }

    public async Task DeletePartnerInAsync(Guid organizationId, Guid partnerOrganizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.PartnersIn_Delete", new
        {
            OrganizationId = organizationId,
            PartnerOrganizationId = partnerOrganizationId
        });
    }

    public async Task AddPartnerOutAsync(Guid organizationId, Guid partnerOrganizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.PartnersOut_Add", new
        {
            OrganizationId = organizationId,
            PartnerOrganizationId = partnerOrganizationId
        });
    }

    public async Task DeletePartnerOutAsync(Guid organizationId, Guid partnerOrganizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.PartnersOut_Delete", new
        {
            OrganizationId = organizationId,
            PartnerOrganizationId = partnerOrganizationId
        });
    }

    public async Task DeletePartnerSharesByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Partners_DeleteByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }
}

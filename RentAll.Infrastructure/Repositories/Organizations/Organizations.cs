using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Create
    public async Task<Organization> CreateAsync(Organization organization)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_Add", new
        {
            OrganizationCode = organization.OrganizationCode,
            Name = organization.Name,
            Address1 = organization.Address1,
            Address2 = organization.Address2,
            Suite = organization.Suite,
            City = organization.City,
            State = organization.State,
            Zip = organization.Zip,
            Phone = organization.Phone,
            Fax = organization.Fax,
            Website = organization.Website,
            LogoPath = organization.LogoPath,
            IsInternational = organization.IsInternational,
            IsActive = organization.IsActive,
            CreatedBy = organization.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Organization not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<IEnumerable<Organization>> GetAllAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetAll", null);

        if (res == null || !res.Any())
            return Enumerable.Empty<Organization>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Organization?> GetByIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetById", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Organization?> GetByOrganizationCodeAsync(string organizationCode)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetByOrganizationCode", new
        {
            OrganizationCode = organizationCode
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsByOrganizationCodeAsync(string organizationCode)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetByOrganizationCode", new
        {
            OrganizationCode = organizationCode
        });

        return res != null && res.Any();
    }
    #endregion

    #region Update
    public async Task<Organization> UpdateByIdAsync(Organization organization)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_UpdateById", new
        {
            OrganizationId = organization.OrganizationId,
            OrganizationCode = organization.OrganizationCode,
            Name = organization.Name,
            Address1 = organization.Address1,
            Address2 = organization.Address2,
            Suite = organization.Suite,
            City = organization.City,
            State = organization.State,
            Zip = organization.Zip,
            Phone = organization.Phone,
            Fax = organization.Fax,
            Website = organization.Website,
            LogoPath = organization.LogoPath,
            IsInternational = organization.IsInternational,
            IsActive = organization.IsActive,
            ModifiedBy = organization.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Organization not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Organization> UpdateColorAsync(Guid organizationId, string rgb, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Color_UpdateById", new
        {
            OrganizationId = organizationId,
            ColorRgb = rgb,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Organization not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Delete
    public async Task DeleteByIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Organization_DeleteById", new
        {
            OrganizationId = organizationId
        });
    }
    #endregion
}

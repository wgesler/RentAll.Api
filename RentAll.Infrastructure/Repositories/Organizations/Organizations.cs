using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

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
            ContactName = organization.ContactName,
            ContactEmail = organization.ContactEmail,
            Website = organization.Website,
            LogoPath = organization.LogoPath,
            IsInternational = organization.IsInternational,
            CurrentInvoiceNo = organization.CurrentInvoiceNo,
            OfficeFee = organization.OfficeFee,
            UserFee = organization.UserFee,
            Unit50Fee = organization.Unit50Fee,
            Unit100Fee = organization.Unit100Fee,
            Unit200Fee = organization.Unit200Fee,
            Unit500Fee = organization.Unit500Fee,
            SendGridName = organization.SendGridName,
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
            ContactName = organization.ContactName,
            ContactEmail = organization.ContactEmail,
            Website = organization.Website,
            LogoPath = organization.LogoPath,
            IsInternational = organization.IsInternational,
            CurrentInvoiceNo = organization.CurrentInvoiceNo,
            OfficeFee = organization.OfficeFee,
            UserFee = organization.UserFee,
            Unit50Fee = organization.Unit50Fee,
            Unit100Fee = organization.Unit100Fee,
            Unit200Fee = organization.Unit200Fee,
            Unit500Fee = organization.Unit500Fee,
            SendGridName = organization.SendGridName,
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

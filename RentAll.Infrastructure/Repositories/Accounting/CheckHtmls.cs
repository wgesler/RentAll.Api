using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region CheckHtml
    public async Task<CheckHtml?> GetCheckHtmlByScopeAsync(Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CheckHtmlEntity>("Accounting.CheckHtml_GetByScope", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertCheckHtmlEntityToModel(res.First());
    }

    public async Task<CheckHtml?> GetCheckHtmlByIdAsync(Guid checkHtmlId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CheckHtmlEntity>("Accounting.CheckHtml_GetById", new
        {
            CheckHtmlId = checkHtmlId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertCheckHtmlEntityToModel(res.First());
    }

    public async Task<List<CheckHtml>> GetCheckHtmlAllAsync(Guid? organizationId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CheckHtmlEntity>("Accounting.CheckHtml_GetAll", new
        {
            OrganizationId = organizationId
        });

        return (res ?? Enumerable.Empty<CheckHtmlEntity>())
            .Select(ConvertCheckHtmlEntityToModel)
            .ToList();
    }

    public async Task<CheckHtml> CreateCheckHtmlAsync(CheckHtml checkHtml)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CheckHtmlEntity>("Accounting.CheckHtml_Add", new
        {
            OrganizationId = checkHtml.OrganizationId,
            OfficeId = checkHtml.OfficeId,
            Check = checkHtml.Check,
            CheckStockPath = checkHtml.CheckStockPath,
            CreatedBy = checkHtml.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("CheckHtml not created");

        return ConvertCheckHtmlEntityToModel(res.First());
    }

    public async Task<CheckHtml> UpdateCheckHtmlByIdAsync(CheckHtml checkHtml)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CheckHtmlEntity>("Accounting.CheckHtml_UpdateById", new
        {
            CheckHtmlId = checkHtml.CheckHtmlId,
            OrganizationId = checkHtml.OrganizationId,
            OfficeId = checkHtml.OfficeId,
            Check = checkHtml.Check,
            CheckStockPath = checkHtml.CheckStockPath,
            ModifiedBy = checkHtml.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("CheckHtml not found");

        return ConvertCheckHtmlEntityToModel(res.First());
    }

    public async Task DeleteCheckHtmlByIdAsync(Guid checkHtmlId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.CheckHtml_DeleteById", new
        {
            CheckHtmlId = checkHtmlId
        });
    }

    private static CheckHtml ConvertCheckHtmlEntityToModel(CheckHtmlEntity entity)
    {
        return new CheckHtml
        {
            CheckHtmlId = entity.CheckHtmlId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            Check = entity.Check,
            CheckStockPath = entity.CheckStockPath,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            ModifiedOn = entity.ModifiedOn,
            ModifiedBy = entity.ModifiedBy
        };
    }
    #endregion
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Accounting;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<NumberCredential?> GetNumberCredentialByOfficeIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<NumberCredentialEntity>("Accounting.NumberCredential_GetByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertEntityToModel(entity);
    }

    public async Task<NumberCredential> AddNumberCredentialAsync(NumberCredential credential)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<NumberCredentialEntity>("Accounting.NumberCredential_Add", new
        {
            OrganizationId = credential.OrganizationId,
            OfficeId = credential.OfficeId,
            SessKey = credential.SessKey,
            MerchId = credential.MerchId,
            ExpiresOn = credential.ExpiresOn
        });

        var entity = res?.FirstOrDefault();
        if (entity == null)
            throw new InvalidOperationException("Number credential was not created.");
        return ConvertEntityToModel(entity);
    }

    public async Task<NumberCredential> UpdateNumberCredentialByOfficeIdAsync(NumberCredential credential)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<NumberCredentialEntity>("Accounting.NumberCredential_UpdateByOfficeId", new
        {
            OrganizationId = credential.OrganizationId,
            OfficeId = credential.OfficeId,
            SessKey = credential.SessKey,
            MerchId = credential.MerchId,
            ExpiresOn = credential.ExpiresOn
        });

        var entity = res?.FirstOrDefault();
        if (entity == null)
            throw new InvalidOperationException("Number credential was not found.");
        return ConvertEntityToModel(entity);
    }

    private static NumberCredential ConvertEntityToModel(NumberCredentialEntity entity)
    {
        return new NumberCredential
        {
            NumberCredentialId = entity.NumberCredentialId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            SessKey = entity.SessKey,
            MerchId = entity.MerchId,
            ExpiresOn = entity.ExpiresOn,
            CreatedOn = entity.CreatedOn,
            ModifiedOn = entity.ModifiedOn
        };
    }
}

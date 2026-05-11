using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Property Listing Share
        public async Task<PropertyListingShare> UpsertPropertyListingShareByPropertyIdAsync(PropertyListingShare share, Guid createdBy)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListingShareEntity>("Property.PropertyListingShare_UpsertByPropertyId", new
            {
                PropertyId = share.PropertyId,
                ShareId = share.ShareId,
                TokenHash = share.TokenHash,
                ExpiresOn = share.ExpiresOn,
                CreatedBy = createdBy
            });

            if (res == null || !res.Any())
                throw new InvalidOperationException("Property listing share was not created.");

            return ConvertEntityToModel(res.First());
        }

        public async Task<PropertyListingShare?> GetPropertyListingShareByTokenHashAsync(string tokenHash)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListingShareEntity>("Property.PropertyListingShare_GetByTokenHash", new
            {
                TokenHash = tokenHash,
                NowUtc = DateTimeOffset.UtcNow
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.First());
        }

        public async Task RevokePropertyListingShareByPropertyIdAsync(Guid propertyId, Guid modifiedBy)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.PropertyListingShare_RevokeByPropertyId", new
            {
                PropertyId = propertyId,
                ModifiedBy = modifiedBy
            });
        }
        #endregion
    }
}

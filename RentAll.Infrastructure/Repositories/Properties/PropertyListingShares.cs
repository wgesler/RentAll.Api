using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Property Listing Share
        public async Task<PropertyListingShare> UpsertPropertyListingShareByPropertyIdAsync(PropertyListingShare share)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListingShareEntity>("Property.PropertyListingShare_UpsertByPropertyId", new
            {
                PropertyId = share.PropertyId,
                ShareId = share.ShareId,
                TokenHash = share.TokenHash,
                ExpiresOn = share.ExpiresOn
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

        public async Task RevokePropertyListingShareByPropertyIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.PropertyListingShare_RevokeByPropertyId", new
            {
                PropertyId = propertyId
            });
        }

        public async Task DeleteExpiredPropertyListingSharesAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.PropertyListingShare_DeleteExpired", new
            {
                NowUtc = DateTimeOffset.UtcNow
            });
        }
        #endregion
    }
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        public async Task<PropertyPhotoImport> CreatePropertyPhotoImportAsync(PropertyPhotoImport import, IReadOnlyList<PropertyPhotoImportItem> items)
        {
            await using var db = new SqlConnection(_dbConnectionString);

            var res = await db.DapperProcQueryAsync<PropertyPhotoImportEntity>("Property.PropertyPhotoImport_Add", new
            {
                ImportId = import.ImportId,
                OrganizationId = import.OrganizationId,
                OfficeId = import.OfficeId,
                VendorId = import.VendorId,
                PropertyId = import.PropertyId,
                PropertyCode = import.PropertyCode
            });

            foreach (var item in items)
            {
                await db.DapperProcExecuteAsync("Property.PropertyPhotoImportItem_Add", new
                {
                    ImportId = import.ImportId,
                    ItemIndex = item.ItemIndex,
                    Url = item.Url,
                    SortOrder = item.SortOrder
                });
            }

            return ConvertEntityToModel(res!.First());
        }

        public async Task<PropertyPhotoImport?> GetPropertyPhotoImportByIdAsync(Guid importId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyPhotoImportEntity>("Property.PropertyPhotoImport_GetById", new
            {
                ImportId = importId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.First());
        }

        public async Task<IEnumerable<PropertyPhotoImportItem>> GetPropertyPhotoImportItemsByImportIdAsync(Guid importId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyPhotoImportItemEntity>("Property.PropertyPhotoImportItem_GetByImportId", new
            {
                ImportId = importId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyPhotoImportItem>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<PropertyPhotoImportClaim?> ClaimNextPropertyPhotoImportItemAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyPhotoImportClaimEntity>("Property.PropertyPhotoImportItem_ClaimNext");

            if (res == null || !res.Any())
                return null;

            var entity = res.First();
            return new PropertyPhotoImportClaim
            {
                Item = ConvertEntityToModel(entity),
                OrganizationId = entity.OrganizationId,
                OfficeId = entity.OfficeId,
                VendorId = entity.VendorId,
                PropertyId = entity.PropertyId,
                PropertyCode = entity.PropertyCode,
                OfficeName = entity.OfficeName
            };
        }

        public async Task CompletePropertyPhotoImportItemAsync(int importItemId, PropertyPhotoImportItemStatus status, int? photoId, string? errorMessage)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.PropertyPhotoImportItem_Complete", new
            {
                ImportItemId = importItemId,
                Status = (byte)status,
                PhotoId = photoId,
                ErrorMessage = errorMessage
            });
        }
    }
}

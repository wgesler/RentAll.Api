using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Selects
        public async Task<PropertyPhoto?> GetPropertyPhotoByIdAsync(int photoId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyPhotoEntity>("Property.Photo_GetById", new
            {
                PhotoId = photoId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.First());
        }

        public async Task<IEnumerable<PropertyPhoto>> GetPropertyPhotosByPropertyIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyPhotoEntity>("Property.Photo_GetAllByPropertyId", new
            {
                PropertyId = propertyId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyPhoto>();

            return res.Select(ConvertEntityToModel);
        }
        #endregion

        #region Creates
        public async Task<PropertyPhoto> CreatePropertyPhotoAsync(PropertyPhoto photo)
        {
            await using var db = new SqlConnection(_dbConnectionString);

            await db.DapperProcExecuteAsync("Property.Photo_Add", new
            {
                PropertyId = photo.PropertyId,
                Order = photo.Order,
                PhotoPath = photo.PhotoPath
            });

            var res = await db.DapperProcQueryAsync<PropertyPhotoEntity>("Property.Photo_GetAllByPropertyId", new
            {
                PropertyId = photo.PropertyId
            });

            if (res == null || !res.Any())
            {
                return new PropertyPhoto
                {
                    PropertyId = photo.PropertyId,
                    Order = photo.Order,
                    PhotoPath = photo.PhotoPath
                };
            }

            var entity = res
                .Where(x => string.Equals(x.PhotoPath, photo.PhotoPath, StringComparison.OrdinalIgnoreCase) && x.Order == photo.Order)
                .OrderByDescending(x => x.PhotoId)
                .FirstOrDefault();

            if (entity == null)
                entity = res.OrderByDescending(x => x.PhotoId).First();

            return ConvertEntityToModel(entity);
        }
        #endregion

        #region Updates
        public async Task UpdatePropertyPhotoOrderAsync(int photoId, int order)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.Photo_Update", new
            {
                PhotoId = photoId,
                Order = order
            });
        }
        #endregion

        #region Deletes
        public async Task DeletePropertyPhotoByIdAsync(int photoId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.Photo_Delete", new
            {
                PhotoId = photoId
            });
        }
        #endregion

    }
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Photos;

namespace RentAll.Infrastructure.Repositories.Photos;

public partial class PhotoRepository : IPhotoRepository
{
    public async Task<Photo?> GetByIdAsync(Guid photoId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PhotoEntity>("Organization.Photo_GetById", new
        {
            PhotoId = photoId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
}

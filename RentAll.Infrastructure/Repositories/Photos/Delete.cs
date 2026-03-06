using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Photos;

public partial class PhotoRepository : IPhotoRepository
{
    public async Task DeleteByIdAsync(Guid photoId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Photo_DeleteById", new
        {
            PhotoId = photoId,
            OrganizationId = organizationId
        });
    }
}

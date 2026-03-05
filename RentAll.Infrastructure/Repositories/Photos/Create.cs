using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Photos;

namespace RentAll.Infrastructure.Repositories.Photos;

public partial class PhotoRepository : IPhotoRepository
{
    public async Task<Photo> CreateAsync(Photo photo)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PhotoEntity>("Organization.Photo_Add", new
        {
            OrganizationId = photo.OrganizationId,
            OfficeId = photo.OfficeId,
            MaintenanceId = photo.MaintenanceId,
            FileName = photo.FileName,
            FileExtension = photo.FileExtension,
            ContentType = photo.ContentType,
            PhotoPath = photo.PhotoPath,
            CreatedBy = photo.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Photo not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Photos;

namespace RentAll.Infrastructure.Repositories.Documents;

public partial class PhotoRepository : IPhotoRepository
{
    private readonly string _dbConnectionString;

    public PhotoRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    private Photo ConvertEntityToModel(PhotoEntity e)
    {
        return new Photo
        {
            PhotoId = e.PhotoId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            MaintenanceId = e.MaintenanceId,
            PhotoPath = e.PhotoPath,
            CreatedBy = e.CreatedBy,
            CreatedOn = e.CreatedOn
        };
    }

    #region Selects
    public async Task<Photo?> GetByIdAsync(Guid photoId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PhotoEntity>("Maintenance.Photo_GetById", new
        {
            PhotoId = photoId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<Photo> CreateAsync(Photo photo)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PhotoEntity>("Maintenance.Photo_Add", new
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
    #endregion

    #region Deletes
    public async Task DeleteByIdAsync(Guid photoId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Photo_DeleteById", new
        {
            PhotoId = photoId,
            OrganizationId = organizationId
        });
    }
    #endregion
}

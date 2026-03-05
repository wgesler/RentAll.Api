using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Photos;

namespace RentAll.Infrastructure.Repositories.Photos;

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
            MaintenanceId = e.MaintenanceId,
            PhotoPath = e.PhotoPath,
            CreatedBy = e.CreatedBy,
            CreatedOn = e.CreatedOn
        };
    }
}

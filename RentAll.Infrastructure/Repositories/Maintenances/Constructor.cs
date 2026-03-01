using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository : IMaintenanceRepository
{
    private readonly string _dbConnectionString;

    public MaintenanceRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    private static Maintenance ConvertEntityToModel(MaintenanceEntity e)
    {
        return new Maintenance
        {
            MaintenanceId = e.MaintenanceId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            InspectionCheckList = e.InspectionCheckList,
            InventoryCheckList = e.InventoryCheckList,
            Notes = e.Notes,
            IsActive = e.IsActive,
            IsDeleted = e.IsDeleted,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    private static Inventory ConvertEntityToModel(InventoryEntity e)
    {
        return new Inventory
        {
            InventoryId = e.InventoryId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            PropertyId = e.PropertyId,
            MaintenanceId = e.MaintenanceId,
            InventoryCheckList = e.InventoryCheckList,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Maintenances;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Property.Property_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<WorkOrder>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<WorkOrder>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<WorkOrder?> GetWorkOrderByIdAsync(int workOrderId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetById", new
        {
            WorkOrderId = workOrderId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_Add", new
        {
            OrganizationId = workOrder.OrganizationId,
            OfficeId = workOrder.OfficeId,
            PropertyId = workOrder.PropertyId,
            Description = workOrder.Description,
            DocumentPath = workOrder.DocumentPath
        });

        if (res == null || !res.Any())
            throw new Exception("Work order record not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_UpdateById", new
        {
            WorkOrderId = workOrder.WorkOrderId,
            OrganizationId = workOrder.OrganizationId,
            OfficeId = workOrder.OfficeId,
            PropertyId = workOrder.PropertyId,
            Description = workOrder.Description,
            DocumentPath = workOrder.DocumentPath
        });

        if (res == null || !res.Any())
            throw new Exception("Work order record not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteWorkOrderByIdAsync(int workOrderId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.WorkOrder_DeleteById", new
        {
            WorkOrderId = workOrderId,
            OrganizationId = organizationId
        });
    }
    #endregion
}

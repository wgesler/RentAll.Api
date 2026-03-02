using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class WorkOrderResponseDto
{
    public int WorkOrderId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentPath { get; set; }

    public WorkOrderResponseDto(WorkOrder workOrder)
    {
        WorkOrderId = workOrder.WorkOrderId;
        OrganizationId = workOrder.OrganizationId;
        OfficeId = workOrder.OfficeId;
        OfficeName = workOrder.OfficeName;
        PropertyId = workOrder.PropertyId;
        PropertyCode = workOrder.PropertyCode;
        Description = workOrder.Description;
        DocumentPath = workOrder.DocumentPath;
    }
}

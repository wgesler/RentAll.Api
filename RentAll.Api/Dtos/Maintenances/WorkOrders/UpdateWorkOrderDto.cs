using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class UpdateWorkOrderDto
{
    public int WorkOrderId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public string? Description { get; set; }
    public string? DocumentPath { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (WorkOrderId <= 0)
            return (false, "WorkOrderId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        return (true, null);
    }

    public WorkOrder ToModel()
    {
        return new WorkOrder
        {
            WorkOrderId = WorkOrderId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            Description = Description,
            DocumentPath = DocumentPath
        };
    }
}

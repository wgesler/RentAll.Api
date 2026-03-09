namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class UpdateWorkOrderDto
{
    public Guid WorkOrderId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public string? Description { get; set; }
    public int WorkOrderTypeId { get; set; }
    public List<UpdateWorkOrderItemDto> WorkOrderItems { get; set; } = new List<UpdateWorkOrderItemDto>();
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (WorkOrderId == Guid.Empty)
            return (false, "WorkOrderId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (!Enum.IsDefined(typeof(WorkOrderType), WorkOrderTypeId))
            return (false, $"Invalid Work Order value: {WorkOrderTypeId}");

        if (WorkOrderItems != null)
        {
            foreach (var item in WorkOrderItems)
            {
                var (isValid, errorMessage) = item.IsValid();
                if (!isValid)
                    return (false, $"WorkOrder Item validation failed: {errorMessage}");
            }
        }

        return (true, null);
    }

    public WorkOrder ToModel(Guid currentUser)
    {
        return new WorkOrder
        {
            WorkOrderId = WorkOrderId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            Description = Description ?? string.Empty,
            WorkOrderType = (WorkOrderType)WorkOrderTypeId,
            WorkOrderItems = WorkOrderItems?.Select(l => l.ToModel(currentUser)).ToList() ?? new List<WorkOrderItem>(),
            IsActive = true,
            ModifiedBy = currentUser
        };
    }
}

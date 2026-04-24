namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class CreateWorkOrderDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public string WorkOrderCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int WorkOrderTypeId { get; set; }
    public bool ApplyMarkup { get; set; }
    public List<CreateWorkOrderItemDto> WorkOrderItems { get; set; } = new List<CreateWorkOrderItemDto>();
    public bool IsActive { get; set; }


    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(WorkOrderCode))
            return (false, "WorkOrderCode is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

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
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            ReservationCode = ReservationCode,
            WorkOrderCode = WorkOrderCode,
            Description = Description,
            WorkOrderType = (WorkOrderType)WorkOrderTypeId,
            ApplyMarkup = ApplyMarkup,
            WorkOrderItems = WorkOrderItems?.Select(l => l.ToModel(currentUser)).ToList() ?? new List<WorkOrderItem>(),
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}

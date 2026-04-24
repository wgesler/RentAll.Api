namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class WorkOrderResponseDto
{
    public Guid WorkOrderId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public string WorkOrderCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int WorkOrderTypeId { get; set; }
    public bool ApplyMarkup { get; set; }
    public List<WorkOrderItem> WorkOrderItems { get; set; } = new List<WorkOrderItem>();
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;

    public WorkOrderResponseDto(WorkOrder workOrder)
    {
        WorkOrderId = workOrder.WorkOrderId;
        OrganizationId = workOrder.OrganizationId;
        OfficeId = workOrder.OfficeId;
        OfficeName = workOrder.OfficeName;
        PropertyId = workOrder.PropertyId;
        PropertyCode = workOrder.PropertyCode;
        ReservationId = workOrder.ReservationId;
        ReservationCode = workOrder.ReservationCode;
        WorkOrderCode = workOrder.WorkOrderCode;
        Description = workOrder.Description;
        WorkOrderTypeId = (int)workOrder.WorkOrderType;
        ApplyMarkup = workOrder.ApplyMarkup;
        WorkOrderItems = workOrder.WorkOrderItems;
        IsActive = workOrder.IsActive;
        CreatedOn = workOrder.CreatedOn;
        ModifiedOn = workOrder.ModifiedOn;
        ModifiedBy = workOrder.ModifiedByName;
    }
}

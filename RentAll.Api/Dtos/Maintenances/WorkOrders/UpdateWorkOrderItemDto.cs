namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class UpdateWorkOrderItemDto
{
    public Guid WorkOrderItemId { get; set; }
    public Guid WorkOrderId { get; set; }
    public string? Description { get; set; }
    public int? ReceiptId { get; set; }
    public int LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ItemAmount { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (WorkOrderItemId == Guid.Empty)
            return (false, "WorkOrderItemId is required");

        if (WorkOrderId == Guid.Empty)
            return (false, "WorkOrderId is required");

        if (ReceiptId.HasValue && ReceiptId.Value <= 0)
            return (false, "ReceiptId must be positive when provided");

        if (ItemAmount <= 0)
            return (false, "ItemAmount is required");

        return (true, null);
    }

    public WorkOrderItem ToModel(Guid currentUser)
    {
        return new WorkOrderItem
        {
            WorkOrderItemId = WorkOrderItemId,
            WorkOrderId = WorkOrderId,
            Description = Description,
            ReceiptId = ReceiptId,
            LaborHours = LaborHours,
            LaborCost = LaborCost,
            ItemAmount = ItemAmount
        };
    }
}

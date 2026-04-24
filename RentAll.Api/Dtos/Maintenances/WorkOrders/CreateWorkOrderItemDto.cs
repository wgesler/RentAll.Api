namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class CreateWorkOrderItemDto
{
    public Guid WorkOrderId { get; set; }
    public string? Description { get; set; }
    public int? ReceiptId { get; set; }
    public int LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ItemAmount { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReceiptId.HasValue && ReceiptId.Value < 0)
            return (false, "ReceiptId must be positive when provided");

        if (ItemAmount == 0)
            return (false, "Work Order Item Amount cannot be zero");

        return (true, null);
    }

    public WorkOrderItem ToModel(Guid currentUser)
    {
        return new WorkOrderItem
        {
            WorkOrderId = WorkOrderId,
            Description = Description,
            ReceiptId = ReceiptId,
            LaborHours = LaborHours,
            LaborCost = LaborCost,
            ItemAmount = ItemAmount
        };
    }
}

namespace RentAll.Api.Dtos.Maintenances.WorkOrders;

public class WorkOrderItemResponseDto
{
    public Guid WorkOrderItemId { get; set; }
    public Guid WorkOrderId { get; set; }
    public string? Description { get; set; }
    public int? ReceiptId { get; set; }
    public int LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ItemAmount { get; set; }

    public WorkOrderItemResponseDto(WorkOrderItem i)
    {
        WorkOrderItemId = i.WorkOrderItemId;
        WorkOrderId = i.WorkOrderId;
        Description = i.Description;
        ReceiptId = i.ReceiptId;
        LaborHours = i.LaborHours;
        LaborCost = i.LaborCost;
        ItemAmount = i.ItemAmount;
    }
}

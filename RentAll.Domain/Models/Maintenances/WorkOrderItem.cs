namespace RentAll.Domain.Models;

public class WorkOrderItem
{
    public Guid WorkOrderItemId { get; set; }
    public Guid WorkOrderId { get; set; }
    public string? Description { get; set; }
    public Guid? ReceiptId { get; set; }
    public int LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ItemAmount { get; set; }
}

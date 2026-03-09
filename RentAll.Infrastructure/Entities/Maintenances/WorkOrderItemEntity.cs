namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class WorkOrderItemEntity
    {
        public Guid WorkOrderItemId { get; set; }
        public Guid WorkOrderId { get; set; }
        public string? Description { get; set; }
        public int? ReceiptId { get; set; }
        public int LaborHours { get; set; }
        public decimal LaborCost { get; set; }
        public decimal ItemAmount { get; set; }
    }
}

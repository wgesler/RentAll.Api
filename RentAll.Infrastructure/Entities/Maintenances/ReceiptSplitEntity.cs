namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class ReceiptSplitEntity
    {
        public int ReceiptSplitId { get; set; }
        public Guid ReceiptId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int ReceiptTypeId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? WorkOrderId { get; set; }
        public string? WorkOrderCode { get; set; }
        public int? ChartOfAccountId { get; set; }
        public string ChartOfAccountDisplayName { get; set; } = string.Empty;
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
        public Guid ModifiedBy { get; set; }
    }
}

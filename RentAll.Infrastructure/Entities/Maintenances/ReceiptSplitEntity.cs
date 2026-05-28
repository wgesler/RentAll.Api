namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class ReceiptSplitEntity
    {
        public int ReceiptSplitId { get; set; }
        public int ReceiptId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int ReceiptTypeId { get; set; }
        public int? BankCardId { get; set; }
        public Guid? WorkOrderId { get; set; }
        public Guid? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? WorkOrderCode { get; set; }
        public string? BankCardDisplayName { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
        public Guid ModifiedBy { get; set; }
    }
}

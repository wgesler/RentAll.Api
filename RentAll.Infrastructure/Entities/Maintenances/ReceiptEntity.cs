namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class ReceiptEntity
    {
        public Guid ReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public Guid OrganizationId { get; set; }
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public string Properties { get; set; } = "[]";
        public DateOnly ReceiptDate { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly AccountingPeriod { get; set; }
        public string? BillNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateOnly? PaidDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? BankCardId { get; set; }
        public string BankCardDisplayName { get; set; } = string.Empty;
        public Guid? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string Splits { get; set; } = "[]";
    public string? ReceiptPath { get; set; }
    public int PaymentTypeId { get; set; }
    public bool CheckPrinted { get; set; }
    public bool IsActive { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTimeOffset ModifiedOn { get; set; }
        public Guid ModifiedBy { get; set; }
        public string ModifiedByName { get; set; } = string.Empty;
    }
}

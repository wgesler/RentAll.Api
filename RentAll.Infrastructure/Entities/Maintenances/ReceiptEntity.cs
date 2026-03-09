namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class ReceiptEntity
    {
        public int ReceiptId { get; set; }
        public Guid OrganizationId { get; set; }
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public Guid PropertyId { get; set; }
        public string PropertyCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string? ReceiptPath { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
        public Guid ModifiedBy { get; set; }
        public string ModifiedByName { get; set; } = string.Empty;
    }
}

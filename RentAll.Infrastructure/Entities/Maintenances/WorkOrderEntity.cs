namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class WorkOrderEntity
    {
        public Guid WorkOrderId { get; set; }
        public Guid OrganizationId { get; set; }
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public Guid PropertyId { get; set; }
        public string PropertyCode { get; set; } = string.Empty;
        public Guid? ReservationId { get; set; }
        public string? ReservationCode { get; set; }
        public int WorkOrderTypeId { get; set; }
        public string WorkOrderCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? WorkOrderItems { get; set; }
        public bool ApplyMarkup { get; set; }
        public DateOnly WorkOrderDate { get; set; }
        public bool UseDepartureFee { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
        public Guid ModifiedBy { get; set; }
        public string ModifiedByName { get; set; } = string.Empty;
    }
}

namespace RentAll.Infrastructure.Entities.Maintenances
{
    public class WorkOrderEntity
    {
        public int WorkOrderId { get; set; }
        public Guid OrganizationId { get; set; }
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public Guid PropertyId { get; set; }
        public string PropertyCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DocumentPath { get; set; }
    }
}

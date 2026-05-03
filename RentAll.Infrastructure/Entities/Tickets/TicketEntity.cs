namespace RentAll.Infrastructure.Entities.Tickets
{
    public class TicketEntity
    {
        public Guid TicketId { get; set; }
        public Guid OrganizationId { get; set; }
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public string? PropertyCode { get; set; }
        public Guid? ReservationId { get; set; }
        public string? ReservationCode { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? Assignee { get; set; }
        public Guid? AgentId { get; set; }
        public string? Agent { get; set; }
        public int TicketStateId { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool NeedPermissionToEnter { get; set; }
        public bool PermissionGranted { get; set; }
        public bool OwnerContacted { get; set; }
        public bool ConfirmedWithTenant { get; set; }
        public bool FollowedUpWithOwner { get; set; }
        public bool WorkOrderCompleted { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
        public Guid ModifiedBy { get; set; }
        public string ModifiedByName { get; set; } = string.Empty;
    }
}

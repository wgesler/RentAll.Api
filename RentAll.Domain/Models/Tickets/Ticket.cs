using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Ticket
{
    public Guid TicketId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStateType TicketStateType { get; set; }
    public bool PermissionToEnter { get; set; }
    public bool OwnerContacted { get; set; }
    public bool ConfirmedWithTenant { get; set; }
    public bool FollowedUpWithOwner { get; set; }
    public bool WorkOrderCompleted { get; set; }
    public List<TicketNote> Notes { get; set; } = new List<TicketNote>();
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}

namespace RentAll.Api.Dtos.Tickets.Tickets;

public class TicketResponseDto
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
    public string TicketCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TicketStateTypeId { get; set; }
    public bool PermissionToEnter { get; set; }
    public bool OwnerContacted { get; set; }
    public bool ConfirmedWithTenant { get; set; }
    public bool FollowedUpWithOwner { get; set; }
    public bool WorkOrderCompleted { get; set; }
    public List<TicketNoteResponseDto> Notes { get; set; } = new List<TicketNoteResponseDto>();
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;

    public TicketResponseDto(Ticket ticket)
    {
        TicketId = ticket.TicketId;
        OrganizationId = ticket.OrganizationId;
        OfficeId = ticket.OfficeId;
        OfficeName = ticket.OfficeName;
        PropertyId = ticket.PropertyId;
        PropertyCode = ticket.PropertyCode;
        ReservationId = ticket.ReservationId;
        ReservationCode = ticket.ReservationCode;
        AssigneeId = ticket.AssigneeId;
        Assignee = ticket.Assignee;
        TicketCode = ticket.TicketCode;
        Title = ticket.Title;
        Description = ticket.Description;
        TicketStateTypeId = (int)ticket.TicketStateType;
        PermissionToEnter = ticket.PermissionToEnter;
        OwnerContacted = ticket.OwnerContacted;
        ConfirmedWithTenant = ticket.ConfirmedWithTenant;
        FollowedUpWithOwner = ticket.FollowedUpWithOwner;
        WorkOrderCompleted = ticket.WorkOrderCompleted;
        Notes = ticket.Notes.Select(note => new TicketNoteResponseDto(note)).ToList();
        IsActive = ticket.IsActive;
        CreatedOn = ticket.CreatedOn;
        ModifiedOn = ticket.ModifiedOn;
        ModifiedBy = ticket.ModifiedByName;
    }
}

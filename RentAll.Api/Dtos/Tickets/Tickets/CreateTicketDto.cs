using RentAll.Api.Dtos.Tickets.TicketNotes;

namespace RentAll.Api.Dtos.Tickets.Tickets;

public class CreateTicketDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? AgentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TicketStateTypeId { get; set; }
    public bool NeedPermissionToEnter { get; set; }
    public bool PermissionGranted { get; set; }
    public bool OwnerContacted { get; set; }
    public bool ConfirmedWithTenant { get; set; }
    public bool FollowedUpWithOwner { get; set; }
    public bool WorkOrderCompleted { get; set; }
    public List<CreateTicketNoteDto>? Notes { get; set; } = new List<CreateTicketNoteDto>();
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(Title))
            return (false, "Title is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (!Enum.IsDefined(typeof(TicketStateType), TicketStateTypeId))
            return (false, $"Invalid TicketStateType value: {TicketStateTypeId}");

        if (Notes != null)
        {
            foreach (var note in Notes)
            {
                var (isValid, errorMessage) = note.IsValid();
                if (!isValid)
                    return (false, $"TicketNote validation failed: {errorMessage}");
            }
        }

        return (true, null);
    }

    public Ticket ToModel(string code, Guid currentUser)
    {
        return new Ticket
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            ReservationCode = ReservationCode,
            AssigneeId = AssigneeId,
            AgentId = AgentId,
            TicketCode = code,
            Title = Title,
            Description = Description,
            TicketStateType = (TicketStateType)TicketStateTypeId,
            NeedPermissionToEnter = NeedPermissionToEnter,
            PermissionGranted = PermissionGranted,
            OwnerContacted = OwnerContacted,
            ConfirmedWithTenant = ConfirmedWithTenant,
            FollowedUpWithOwner = FollowedUpWithOwner,
            WorkOrderCompleted = WorkOrderCompleted,
            Notes = Notes?.Select(note => note.ToModel(currentUser)).ToList() ?? new List<TicketNote>(),
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

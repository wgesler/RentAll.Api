namespace RentAll.Api.Dtos.Tickets.Tickets;

public class UpdateTicketDto
{
    public Guid TicketId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TicketStateTypeId { get; set; }
    public bool PermissionToEnter { get; set; }
    public bool OwnerContacted { get; set; }
    public bool ConfirmedWithTenant { get; set; }
    public bool FollowedUpWithOwner { get; set; }
    public bool WorkOrderCompleted { get; set; }
    public List<UpdateTicketNoteDto> Notes { get; set; } = new List<UpdateTicketNoteDto>();
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TicketId == Guid.Empty)
            return (false, "TicketId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(TicketCode))
            return (false, "TicketCode is required");

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

    public Ticket ToModel(Guid currentUser)
    {
        return new Ticket
        {
            TicketId = TicketId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            ReservationCode = ReservationCode,
            TicketCode = TicketCode,
            Description = Description,
            TicketStateType = (TicketStateType)TicketStateTypeId,
            PermissionToEnter = PermissionToEnter,
            OwnerContacted = OwnerContacted,
            ConfirmedWithTenant = ConfirmedWithTenant,
            FollowedUpWithOwner = FollowedUpWithOwner,
            WorkOrderCompleted = WorkOrderCompleted,
            Notes = Notes?.Select(note => note.ToModel(currentUser)).ToList() ?? new List<TicketNote>(),
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}

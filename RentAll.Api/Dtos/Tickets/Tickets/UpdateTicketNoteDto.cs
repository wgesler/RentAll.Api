namespace RentAll.Api.Dtos.Tickets.Tickets;

public class UpdateTicketNoteDto
{
    public int TicketNoteId { get; set; }
    public string Note { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TicketNoteId < 0)
            return (false, "TicketNoteId must be zero or greater");

        if (string.IsNullOrWhiteSpace(Note))
            return (false, "Note is required");

        return (true, null);
    }

    public TicketNote ToModel(Guid currentUser)
    {
        return new TicketNote
        {
            TicketNoteId = TicketNoteId,
            Note = Note,
            ModifiedBy = currentUser
        };
    }
}

namespace RentAll.Api.Dtos.Tickets.Tickets;

public class CreateTicketNoteDto
{
    public string Note { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Note))
            return (false, "Note is required");

        return (true, null);
    }

    public TicketNote ToModel(Guid currentUser)
    {
        return new TicketNote
        {
            Note = Note,
            CreatedBy = currentUser
        };
    }
}

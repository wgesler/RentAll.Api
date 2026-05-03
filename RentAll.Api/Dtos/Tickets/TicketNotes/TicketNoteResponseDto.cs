namespace RentAll.Api.Dtos.Tickets.TicketNotes;

public class TicketNoteResponseDto
{
    public int TicketNoteId { get; set; }
    public Guid TicketId { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;

    public TicketNoteResponseDto(TicketNote note)
    {
        TicketNoteId = note.TicketNoteId;
        TicketId = note.TicketId;
        Note = note.Note;
        CreatedOn = note.CreatedOn;
        CreatedBy = note.CreatedBy;
        CreatedByName = note.CreatedByName;
        ModifiedOn = note.ModifiedOn;
        ModifiedBy = note.ModifiedBy;
        ModifiedByName = note.ModifiedByName;
    }
}

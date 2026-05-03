namespace RentAll.Domain.Models;

public class TicketNote
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
}

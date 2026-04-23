namespace RentAll.Domain.Models;

public class TicketNote
{
    public int TicketNoteId { get; set; }
    public Guid TicketId { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

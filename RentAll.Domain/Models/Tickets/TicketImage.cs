namespace RentAll.Domain.Models;

public class TicketImage
{
    public int TicketImageId { get; set; }
    public Guid TicketId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

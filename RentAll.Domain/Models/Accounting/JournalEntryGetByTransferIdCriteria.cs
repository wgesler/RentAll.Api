namespace RentAll.Domain.Models;

public class JournalEntryGetByTransferIdCriteria
{
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
}

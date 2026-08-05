namespace RentAll.Domain.Models;

public class JournalEntryGetByDepositIdCriteria
{
    public Guid OrganizationId { get; set; }
    public Guid DepositId { get; set; }
}

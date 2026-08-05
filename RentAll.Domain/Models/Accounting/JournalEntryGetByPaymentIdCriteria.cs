namespace RentAll.Domain.Models;

public class JournalEntryGetByPaymentIdCriteria
{
    public Guid OrganizationId { get; set; }
    public Guid PaymentId { get; set; }
}

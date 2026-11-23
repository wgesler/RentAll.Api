namespace RentAll.Infrastructure.Entities.Contacts;

public class ContactBankEntity
{
    public int ContactBankId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int BankAccountTypeId { get; set; }
    public string? NameOnAccount { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public byte[]? AccountNumber { get; set; }
    public string? LastFour { get; set; }
    public int? NumberConsentId { get; set; }
    public Guid? ReservationId { get; set; }
    public DateTimeOffset? AuthorizedOn { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

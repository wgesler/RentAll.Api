namespace RentAll.Infrastructure.Entities.Accounting;

public class PaymentLinkEntity
{
    public int PaymentLinkId { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid ReservationId { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
}

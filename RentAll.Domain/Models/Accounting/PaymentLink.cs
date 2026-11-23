namespace RentAll.Domain.Models.Accounting;

/// <summary>A payment request sent by email; the link carries Token instead of the reservation id.</summary>
public class PaymentLink
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

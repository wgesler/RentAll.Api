namespace RentAll.Infrastructure.Entities.Accounting;

public class PaymentPageEntity
{
    public int PaymentPageId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string RequestPageUrl { get; set; } = string.Empty;
    public string ResponsePageUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

namespace RentAll.Domain.Models.Accounting;

/// <summary>
/// Where an organization's payment pages live. OfficeId null applies to every office without its own row; the system
/// organization's row is RentAll's own pages. A URL starting with "/" is a RentAll page (resolved against
/// PaymentPageSettings:AppBaseUrl).
/// </summary>
public class PaymentPage
{
    public int PaymentPageId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string RequestPageUrl { get; set; } = string.Empty;
    public string ResponsePageUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

namespace RentAll.Domain.Models;

public class EmailHtml
{
    public Guid OrganizationId { get; set; }
    public string WelcomeLetter { get; set; } = string.Empty;
    public string CorporateLetter { get; set; } = string.Empty;
    public string Lease { get; set; } = string.Empty;
    public string Invoice { get; set; } = string.Empty;
    public string LetterSubject { get; set; } = string.Empty;
    public string LeaseSubject { get; set; } = string.Empty;
    public string InvoiceSubject { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

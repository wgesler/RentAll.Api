using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Emails.EmailHtmls;

public class CreateEmailHtmlDto
{
    public Guid OrganizationId { get; set; }
    public string WelcomeLetter { get; set; } = string.Empty;
    public string CorporateLetter { get; set; } = string.Empty;
    public string Lease { get; set; } = string.Empty;
    public string Invoice { get; set; } = string.Empty;
    public string LetterSubject { get; set; } = string.Empty;
    public string LeaseSubject { get; set; } = string.Empty;
    public string InvoiceSubject { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organizationId)
    {
        if (OrganizationId == Guid.Empty || OrganizationId != organizationId)
            return (false, "OrganizationId not valid");

        if (string.IsNullOrWhiteSpace(WelcomeLetter))
            return (false, "WelcomeLetter is required");

        if (string.IsNullOrWhiteSpace(CorporateLetter))
            return (false, "CorporateLetter is required");

        if (string.IsNullOrWhiteSpace(Lease))
            return (false, "Lease is required");

        if (string.IsNullOrWhiteSpace(Invoice))
            return (false, "Invoice is required");

        if (string.IsNullOrWhiteSpace(LetterSubject))
            return (false, "LetterSubject is required");

        if (string.IsNullOrWhiteSpace(LeaseSubject))
            return (false, "LeaseSubject is required");

        if (string.IsNullOrWhiteSpace(InvoiceSubject))
            return (false, "InvoiceSubject is required");

        return (true, null);
    }

    public EmailHtml ToModel(Guid currentUser)
    {
        return new EmailHtml
        {
            OrganizationId = OrganizationId,
            WelcomeLetter = WelcomeLetter,
            CorporateLetter = CorporateLetter,
            Lease = Lease,
            Invoice = Invoice,
            LetterSubject = LetterSubject,
            LeaseSubject = LeaseSubject,
            InvoiceSubject = InvoiceSubject,
            CreatedBy = currentUser
        };
    }
}

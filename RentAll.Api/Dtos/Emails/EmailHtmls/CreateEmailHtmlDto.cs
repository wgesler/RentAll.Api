namespace RentAll.Api.Dtos.Emails.EmailHtmls;

public class CreateEmailHtmlDto
{
    public Guid OrganizationId { get; set; }
    public string WelcomeLetter { get; set; } = string.Empty;
    public string DepartureLetter { get; set; } = string.Empty;
    public string CorporateLetter { get; set; } = string.Empty;
    public string Lease { get; set; } = string.Empty;
    public string CorporateLease { get; set; } = string.Empty;
    public string Invoice { get; set; } = string.Empty;
    public string CorporateInvoice { get; set; } = string.Empty;
    public string OwnerStatement { get; set; } = string.Empty;
    public string Schedules { get; set; } = string.Empty;
    public string LetterSubject { get; set; } = string.Empty;
    public string DepartureSubject { get; set; } = string.Empty;
    public string LeaseSubject { get; set; } = string.Empty;
    public string InvoiceSubject { get; set; } = string.Empty;
    public string OwnerStatementSubject { get; set; } = string.Empty;
    public string ScheduleSubject { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organizationId)
    {
        if (OrganizationId == Guid.Empty || OrganizationId != organizationId)
            return (false, "OrganizationId not valid");

        if (string.IsNullOrWhiteSpace(WelcomeLetter))
            return (false, "WelcomeLetter is required");

        if (string.IsNullOrWhiteSpace(DepartureLetter))
            return (false, "DepartureLetter is required");

        if (string.IsNullOrWhiteSpace(CorporateLetter))
            return (false, "CorporateLetter is required");

        if (string.IsNullOrWhiteSpace(Lease))
            return (false, "Lease is required");

        if (string.IsNullOrWhiteSpace(CorporateLease))
            return (false, "CorporateLease is required");

        if (string.IsNullOrWhiteSpace(Invoice))
            return (false, "Invoice is required");

        if (string.IsNullOrWhiteSpace(CorporateInvoice))
            return (false, "CorporateInvoice is required");

        if (string.IsNullOrWhiteSpace(OwnerStatement))
            return (false, "OwnerStatement is required");

        if (string.IsNullOrWhiteSpace(Schedules))
            return (false, "Schedules is required");

        if (string.IsNullOrWhiteSpace(LetterSubject))
            return (false, "LetterSubject is required");

        if (string.IsNullOrWhiteSpace(DepartureSubject))
            return (false, "DepartureSubject is required");

        if (string.IsNullOrWhiteSpace(LeaseSubject))
            return (false, "LeaseSubject is required");

        if (string.IsNullOrWhiteSpace(InvoiceSubject))
            return (false, "InvoiceSubject is required");

        if (string.IsNullOrWhiteSpace(OwnerStatementSubject))
            return (false, "OwnerStatementSubject is required");

        if (string.IsNullOrWhiteSpace(ScheduleSubject))
            return (false, "ScheduleSubject is required");

        return (true, null);
    }

    public EmailHtml ToModel(Guid currentUser)
    {
        return new EmailHtml
        {
            OrganizationId = OrganizationId,
            WelcomeLetter = WelcomeLetter,
            DepartureLetter = DepartureLetter,
            CorporateLetter = CorporateLetter,
            Lease = Lease,
            CorporateLease = CorporateLease,
            Invoice = Invoice,
            CorporateInvoice = CorporateInvoice,
            OwnerStatement = OwnerStatement,
            Schedules = Schedules,
            LetterSubject = LetterSubject,
            DepartureSubject = DepartureSubject,
            LeaseSubject = LeaseSubject,
            InvoiceSubject = InvoiceSubject,
            OwnerStatementSubject = OwnerStatementSubject,
            ScheduleSubject = ScheduleSubject,
            CreatedBy = currentUser
        };
    }
}

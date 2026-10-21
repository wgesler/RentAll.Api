namespace RentAll.Api.Dtos.Organizations.Terminologies;

public class TerminologyUpdateDto
{
    public int TermId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TermId <= 0)
            return (false, "Term ID is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "Office ID is required");

        if (string.IsNullOrWhiteSpace(Term))
            return (false, "Term is required");

        if (Term.Trim().Length > 200)
            return (false, "Term cannot exceed 200 characters");

        if (string.IsNullOrWhiteSpace(Replacement))
            return (false, "Replacement is required");

        if (Replacement.Trim().Length > 200)
            return (false, "Replacement cannot exceed 200 characters");

        return (true, null);
    }

    public Terminology ToModel()
    {
        return new Terminology
        {
            TermId = TermId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            Term = Term.Trim(),
            Replacement = Replacement.Trim()
        };
    }
}

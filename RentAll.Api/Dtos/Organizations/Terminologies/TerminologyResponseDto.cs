namespace RentAll.Api.Dtos.Organizations.Terminologies;

public class TerminologyResponseDto
{
    public int TermId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;

    public TerminologyResponseDto(Terminology terminology)
    {
        TermId = terminology.TermId;
        OrganizationId = terminology.OrganizationId;
        OfficeId = terminology.OfficeId;
        OfficeName = terminology.OfficeName;
        Term = terminology.Term;
        Replacement = terminology.Replacement;
    }
}

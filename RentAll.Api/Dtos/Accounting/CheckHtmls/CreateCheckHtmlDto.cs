namespace RentAll.Api.Dtos.Accounting.CheckHtmls;

public class CreateCheckHtmlDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string Check { get; set; } = "[]";

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organizationId)
    {
        if (OrganizationId == Guid.Empty || OrganizationId != organizationId)
            return (false, "OrganizationId not valid");

        if (string.IsNullOrWhiteSpace(Check))
            return (false, "Check is required");

        return (true, null);
    }

    public CheckHtml ToModel(Guid currentUser)
    {
        return new CheckHtml
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            Check = Check,
            CreatedBy = currentUser
        };
    }
}

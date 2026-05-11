namespace RentAll.Api.Dtos.Leads.Owners;

public class CreateExternalLeadOwnerDto : CreateLeadOwnerDto
{
    public Guid OrganizationId { get; set; }

    public new (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        return base.IsValid();
    }
}

namespace RentAll.Api.Dtos.Leads.Rentals;

public class CreateExternalLeadRentalDto : CreateLeadRentalDto
{
    public Guid OrganizationId { get; set; }

    public new (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        return base.IsValid();
    }
}

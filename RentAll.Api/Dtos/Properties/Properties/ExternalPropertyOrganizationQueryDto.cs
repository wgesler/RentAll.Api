namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyOrganizationQueryDto
{
    public Guid OrganizationId { get; set; }

    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        return (true, null);
    }
}

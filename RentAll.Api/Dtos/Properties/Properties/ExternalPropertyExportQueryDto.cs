namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyExportQueryDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }

    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        return (true, null);
    }
}

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ExternalReservationExportQueryDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }

    public (bool IsValid, string? ErrorMessage) Validate()
    {
        var errors = new List<string>();
        if (OrganizationId == Guid.Empty)
            errors.Add("OrganizationId is required");
        if (OfficeId <= 0)
            errors.Add("OfficeId is required");
        return errors.Count == 0 ? (true, null) : (false, string.Join("\n", errors));
    }
}

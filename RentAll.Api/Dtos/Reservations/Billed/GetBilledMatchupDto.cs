namespace RentAll.Api.Dtos.Reservations.Billed;

public class GetBilledMatchupDto
{
    public string? OfficeIds { get; set; }

    public string ResolvedOfficeIds => (OfficeIds ?? string.Empty).Trim();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(ResolvedOfficeIds))
            return (false, "OfficeIds is required");
        return (true, null);
    }
}

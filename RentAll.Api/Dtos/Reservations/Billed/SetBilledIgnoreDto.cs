namespace RentAll.Api.Dtos.Reservations.Billed;

public class SetBilledIgnoreDto
{
    public bool Ignore { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        return (true, null);
    }
}

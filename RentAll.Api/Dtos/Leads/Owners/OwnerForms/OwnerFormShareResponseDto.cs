namespace RentAll.Api.Dtos.Leads.Owners;

public class OwnerFormShareResponseDto
{
    public Guid ShareId { get; set; }
    public int OwnerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
}

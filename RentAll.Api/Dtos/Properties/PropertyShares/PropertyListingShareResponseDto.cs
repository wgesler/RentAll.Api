namespace RentAll.Api.Dtos.Properties.PropertyShares;

public class PropertyListingShareResponseDto
{
    public Guid ShareId { get; set; }
    public Guid PropertyId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
}

namespace RentAll.Infrastructure.Entities;

public class RefreshTokenEntity
{
	public Guid RefreshTokenId { get; set; }
	public Guid UserId { get; set; }
	public string TokenHash { get; set; } = string.Empty;
	public DateTimeOffset ExpiresOn { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
}


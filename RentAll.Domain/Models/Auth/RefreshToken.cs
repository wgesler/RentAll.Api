namespace RentAll.Domain.Models.Auth;

public class RefreshToken
{
	public Guid RefreshTokenId { get; set; }
	public Guid UserId { get; set; }
	public string TokenHash { get; set; } = string.Empty;
	public DateTimeOffset ExpiresOn { get; set; }
	public DateTimeOffset CreatedOn { get; set; }

	public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresOn;
	public bool IsActive => !IsExpired;
}


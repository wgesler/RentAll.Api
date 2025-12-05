namespace RentAll.Api.Dtos.Auth;

public class AuthResponseDto
{
<<<<<<< Updated upstream
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
=======
	public string? AccessToken { get; set; }
	public int? ExpiresIn { get; set; }
	public string TokenType { get; set; } = "Bearer";
	public string? RefreshToken { get; set; }
	public string Scope { get; set; } = "OAuth 2.0";

	public AuthResponseDto(string accessToken, string refreshToken, int expiresInSeconds)
	{
		AccessToken = accessToken;
		RefreshToken = refreshToken;
		ExpiresIn = expiresInSeconds;
		TokenType = "Bearer";
		Scope = "OAuth 2.0";
	}
>>>>>>> Stashed changes
}



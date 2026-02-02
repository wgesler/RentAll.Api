namespace RentAll.Api.Dtos.Auth;

public class AuthResponseDto
{
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
}



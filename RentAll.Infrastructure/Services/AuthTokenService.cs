using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Models.Users;

namespace RentAll.Infrastructure.Services;

public class AuthTokenService : IAuthTokenService
{
    private readonly IConfiguration _configuration;

    public AuthTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? "RentAll.Api";
        var audience = jwtSettings["Audience"] ?? "RentAll.Api";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Generate session GUID for sub claim
        var sessionGuid = Guid.NewGuid().ToString();

        // Create user object with GUID and userGroups (roles)
        // userGroups is an array, even if there's only one role
        var userGroups = string.IsNullOrEmpty(user.Role) 
            ? new List<string>() 
            : new List<string> { user.Role };

        var userObject = new
        {
            userGuid = user.UserId.ToString(),
			firstName = user.FirstName,
			lastName = user.LastName,
            email = user.Email,
			userGroups = userGroups
		};

        // Serialize and base64 encode the user object
        var userJson = JsonSerializer.Serialize(userObject);
        var userBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(userJson));

        var claims = new[]
        {
            new Claim("sub", sessionGuid),
            new Claim("user", userBase64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
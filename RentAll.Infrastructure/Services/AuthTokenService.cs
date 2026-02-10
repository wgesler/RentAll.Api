using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

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

        // Convert UserGroups List<string> to comma-delimited string
        var userGroupsString = user.UserGroups != null && user.UserGroups.Any()
            ? string.Join(",", user.UserGroups)
            : string.Empty;

        // Convert OfficeAccess List<int> to comma-delimited string
        var officeAccessString = user.OfficeAccess != null && user.OfficeAccess.Any()
            ? string.Join(",", user.OfficeAccess)
            : string.Empty;

        var userObject = new
        {
            userId = user.UserId.ToString(),
			organizationId = user.OrganizationId.ToString(),
			firstName = user.FirstName,
			lastName = user.LastName,
            email = user.Email,
			userGroups = userGroupsString,
			officeAccess = officeAccessString,
			startupPageId = (int)user.StartupPage
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

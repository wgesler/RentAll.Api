using RentAll.Domain.Models.Users;

namespace RentAll.Api.Dtos.Auth;

public class JwtUserResponseDto
{
    public Guid UserGuid { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public JwtUserResponseDto(User user)
    {
		UserGuid = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        FullName = user.FullName;
        Email = user.Email;
        Role = user.Role;
    }
}
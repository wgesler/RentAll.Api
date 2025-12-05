using RentAll.Domain.Models.Users;

namespace RentAll.Api.Dtos.Auth;

public class UserResponseDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }

    public UserResponseDto(User user)
    {
        UserId = user.UserId;
        Username = user.Username;
        FirstName = user.FirstName;
        LastName = user.LastName;
        FullName = user.FullName;
        Email = user.Email;
        IsActive = user.IsActive;
        CreatedOn = user.CreatedOn;
		ModifiedOn = user.ModifiedOn;
    }
}




namespace RentAll.Domain.Models;

public class User
{
    public Guid UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public int IsActive { get; set; } = 1;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}



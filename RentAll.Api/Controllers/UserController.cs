using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("user")]
	public partial class UserController : ControllerBase
	{
		private readonly IUserRepository _userRepository;
		private readonly IPasswordHasher _passwordHasher;
		private readonly ILogger<UserController> _logger;

		protected Guid CurrentUser { get; private set; }

		public UserController(
			IUserRepository userRepository,
			IPasswordHasher passwordHasher,
			ILogger<UserController> logger)
		{
			_userRepository = userRepository;
			_passwordHasher = passwordHasher;
			_logger = logger;

			// Extract user ID from JWT claims on instantiation
			CurrentUser = GetCurrentUserIdFromJwt();
		}

		private Guid GetCurrentUserIdFromJwt()
		{
			if (User?.Identity?.IsAuthenticated != true)
				return Guid.Empty;

			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
				return Guid.Empty;

			return userId;
		}
	}
}


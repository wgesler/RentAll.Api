using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("user")]
	//[Authorize]
	public partial class UserController : BaseController
	{
		private readonly IUserRepository _userRepository;
		private readonly IPasswordHasher _passwordHasher;
		private readonly ILogger<UserController> _logger;

		public UserController(
			IUserRepository userRepository,
			IPasswordHasher passwordHasher,
			ILogger<UserController> logger)
		{
			_userRepository = userRepository;
			_passwordHasher = passwordHasher;
			_logger = logger;
		}
	}
}

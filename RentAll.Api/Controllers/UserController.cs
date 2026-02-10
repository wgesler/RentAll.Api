using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/user")]
	//[Authorize]
	public partial class UserController : BaseController
	{
		private readonly IUserRepository _userRepository;
		private readonly IPasswordHasher _passwordHasher;
		private readonly IFileService _fileService;
		private readonly ILogger<UserController> _logger;

		public UserController(
			IUserRepository userRepository,
			IPasswordHasher passwordHasher,
			IFileService fileService,
			ILogger<UserController> logger)
		{
			_userRepository = userRepository;
			_passwordHasher = passwordHasher;
			_fileService = fileService;
			_logger = logger;
		}
	}
}

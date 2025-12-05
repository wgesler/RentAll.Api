using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("rental")]
	public partial class RentalController : ControllerBase
	{
		private readonly IRentalRepository _rentalRepository;
		private readonly ILogger<RentalController> _logger;

		protected Guid CurrentUser { get; private set; }

		public RentalController(
			IRentalRepository rentalRepository,
			ILogger<RentalController> logger)
		{
			_rentalRepository = rentalRepository;
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


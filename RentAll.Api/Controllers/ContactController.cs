using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("contact")]
    public partial class ContactController : ControllerBase
    {
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<ContactController> _logger;

        protected Guid CurrentUser { get; private set; }

        public ContactController(
            IContactRepository contactRepository,
            ILogger<ContactController> logger)
        {
            _contactRepository = contactRepository;
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
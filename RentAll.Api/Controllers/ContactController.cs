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

        protected Guid CurrentUser => GetCurrentUserIdFromJwt();

        public ContactController(
            IContactRepository contactRepository,
            ILogger<ContactController> logger)
        {
            _contactRepository = contactRepository;
            _logger = logger;
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

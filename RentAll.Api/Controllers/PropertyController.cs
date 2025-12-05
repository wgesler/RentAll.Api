using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("property")]
    public partial class PropertyController : ControllerBase
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<PropertyController> _logger;

        protected Guid CurrentUser { get; private set; }

        public PropertyController(
            IPropertyRepository propertyRepository,
            ILogger<PropertyController> logger)
        {
            _propertyRepository = propertyRepository;
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
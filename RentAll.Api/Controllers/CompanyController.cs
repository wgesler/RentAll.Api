using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("company")]
    public partial class CompanyController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<CompanyController> _logger;

        protected Guid CurrentUser { get; private set; }

        public CompanyController(
            ICompanyRepository companyRepository,
            ILogger<CompanyController> logger)
        {
            _companyRepository = companyRepository;
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
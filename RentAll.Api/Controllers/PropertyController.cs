using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("property")]
    [Authorize]
    public partial class PropertyController : BaseController
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyRepository propertyRepository,
            ILogger<PropertyController> logger)
        {
            _propertyRepository = propertyRepository;
            _logger = logger;
        }
    }
}
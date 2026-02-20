using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/propertyhtml")]
    [Authorize]
    public partial class PropertyHtmlController : BaseController
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<PropertyHtmlController> _logger;

        public PropertyHtmlController(
            IPropertyRepository propertyRepository,
            ILogger<PropertyHtmlController> logger)
        {
            _propertyRepository = propertyRepository;
            _logger = logger;
        }
    }
}



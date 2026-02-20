using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/propertyletter")]
    [Authorize]
    public partial class PropertyLetterController : BaseController
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<PropertyLetterController> _logger;

        public PropertyLetterController(
            IPropertyRepository propertyRepository,
            ILogger<PropertyLetterController> logger)
        {
            _propertyRepository = propertyRepository;
            _logger = logger;
        }
    }
}



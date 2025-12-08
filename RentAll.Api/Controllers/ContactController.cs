using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("contact")]
    [Authorize]
    public partial class ContactController : BaseController
    {
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IContactRepository contactRepository,
            ILogger<ContactController> logger)
        {
            _contactRepository = contactRepository;
            _logger = logger;
        }
    }
}

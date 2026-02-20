using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/contact")]
    [Authorize]
    public partial class ContactController : BaseController
    {
        private readonly IContactManager _contactManager;
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IContactManager contactManager,
            IContactRepository contactRepository,
            ILogger<ContactController> logger)
        {
            _contactManager = contactManager;
            _contactRepository = contactRepository;
            _logger = logger;
        }
    }
}

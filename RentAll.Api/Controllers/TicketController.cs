using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/ticket")]
[Authorize]
public partial class TicketController : BaseController
{
    #region Fields
    private readonly IOrganizationManager _organizationManager;
    private readonly ITicketRepository _ticketRepository;
    private readonly IEmailManager _emailManager;
    private readonly ILogger<TicketController> _logger;
    #endregion

    #region Constructor
    public TicketController(
        IOrganizationManager organizationManager,
        ITicketRepository ticketRepository,
        IEmailManager emailManager,
        ILogger<TicketController> logger)
    {
        _organizationManager = organizationManager;
        _ticketRepository = ticketRepository;
        _emailManager = emailManager;
        _logger = logger;
    }
    #endregion
}

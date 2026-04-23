using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/ticket")]
[Authorize]
public partial class TicketController : BaseController
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ITicketRepository ticketRepository,
        ILogger<TicketController> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }
}

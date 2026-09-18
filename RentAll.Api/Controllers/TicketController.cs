using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/ticket")]
[Authorize]
public partial class TicketController : BaseController
{
    #region Fields
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationManager _organizationManager;
    private readonly ITicketRepository _ticketRepository;
    private readonly IEmailManager _emailManager;
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly ExternalPropertyUploadLogService _externalPropertyUploadLogService;
    private readonly ILogger<TicketController> _logger;
    #endregion

    #region Constructor
    public TicketController(
        IOrganizationRepository organizationRepository,
        IOrganizationManager organizationManager,
        ITicketRepository ticketRepository,
        IEmailManager emailManager,
        IExternalApiKeyService externalApiKeyService,
        ExternalPropertyUploadLogService externalPropertyUploadLogService,
        ILogger<TicketController> logger)
    {
        _organizationRepository = organizationRepository;
        _organizationManager = organizationManager;
        _ticketRepository = ticketRepository;
        _emailManager = emailManager;
        _externalApiKeyService = externalApiKeyService;
        _externalPropertyUploadLogService = externalPropertyUploadLogService;
        _logger = logger;
    }
    #endregion
}

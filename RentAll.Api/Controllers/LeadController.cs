using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize]
public partial class LeadController : BaseController
{
    private readonly ILeadRepository _leadRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ILeadRepository leadRepository,
        IOrganizationRepository organizationRepository,
        ILogger<LeadController> logger)
    {
        _leadRepository = leadRepository;
        _organizationRepository = organizationRepository;
        _logger = logger;
    }
}

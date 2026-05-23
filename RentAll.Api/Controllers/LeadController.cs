using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize]
public partial class LeadController : BaseController
{
    private readonly ILeadRepository _leadRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ILeadRepository leadRepository,
        IPropertyRepository propertyRepository,
        IOrganizationRepository organizationRepository,
        ILogger<LeadController> logger)
    {
        _leadRepository = leadRepository;
        _propertyRepository = propertyRepository;
        _organizationRepository = organizationRepository;
        _logger = logger;
    }
}

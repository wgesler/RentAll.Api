using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize]
public partial class LeadController : BaseController
{
    private readonly ILeadRepository _leadRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly ExternalPropertyUploadLogService _externalPropertyUploadLogService;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ILeadRepository leadRepository,
        IPropertyRepository propertyRepository,
        IOrganizationRepository organizationRepository,
        IExternalApiKeyService externalApiKeyService,
        ExternalPropertyUploadLogService externalPropertyUploadLogService,
        ILogger<LeadController> logger)
    {
        _leadRepository = leadRepository;
        _propertyRepository = propertyRepository;
        _organizationRepository = organizationRepository;
        _externalApiKeyService = externalApiKeyService;
        _externalPropertyUploadLogService = externalPropertyUploadLogService;
        _logger = logger;
    }
}

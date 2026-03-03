using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public partial class MaintenanceController : BaseController
{
    private readonly IOrganizationManager _organizationManager;
    private readonly IMaintenanceManager _maintenanceManager;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IFileService _fileService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IOrganizationManager organizationManager,
        IMaintenanceManager maintenanceManager,
        IMaintenanceRepository maintenanceRepository,
        IPropertyRepository propertyRepository,
        IFileService fileService,
        ILogger<MaintenanceController> logger)
    {
        _organizationManager = organizationManager;
        _maintenanceManager = maintenanceManager;
        _maintenanceRepository = maintenanceRepository;
        _propertyRepository = propertyRepository;
        _fileService = fileService;
        _logger = logger;
    }
}

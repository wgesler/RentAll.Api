using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public partial class MaintenanceController : BaseController
{
    private readonly IMaintenanceManager _maintenanceManager;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceManager maintenanceManager,
        IMaintenanceRepository maintenanceRepository,
        IPropertyRepository propertyRepository,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceManager = maintenanceManager;
        _maintenanceRepository = maintenanceRepository;
        _propertyRepository = propertyRepository;
        _logger = logger;
    }
}

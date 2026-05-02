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
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMaintenanceManager _maintenanceManager;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IFileService _fileService;
    private readonly IFileAttachmentHelper _fileAttachmentHelper;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IOrganizationRepository organizationRepository,
        IMaintenanceManager maintenanceManager,
        IMaintenanceRepository maintenanceRepository,
        IPhotoRepository photoRepository,
        IPropertyRepository propertyRepository,
        IFileService fileService,
        IFileAttachmentHelper fileAttachmentHelper,
        ILogger<MaintenanceController> logger)
    {
        _organizationRepository = organizationRepository;
        _maintenanceManager = maintenanceManager;
        _maintenanceRepository = maintenanceRepository;
        _photoRepository = photoRepository;
        _propertyRepository = propertyRepository;
        _fileService = fileService;
        _fileAttachmentHelper = fileAttachmentHelper;
        _logger = logger;
    }
}

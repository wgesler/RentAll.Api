using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
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
    private readonly IOrganizationManager _organizationManager;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IAccountingManager _accountingManager;
    private readonly IMaintenanceManager _maintenanceManager;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IFileService _fileService;
    private readonly IFileAttachmentHelper _fileAttachmentHelper;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly ReceiptExtractEnrichmentService _receiptExtractEnrichmentService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IOrganizationRepository organizationRepository,
        IOrganizationManager organizationManager,
        IAccountingRepository accountingRepository,
        IAccountingManager accountingManager,
        IMaintenanceManager maintenanceManager,
        IMaintenanceRepository maintenanceRepository,
        IPhotoRepository photoRepository,
        IPropertyRepository propertyRepository,
        IFileService fileService,
        IFileAttachmentHelper fileAttachmentHelper,
        IDocumentIntelligenceService documentIntelligenceService,
        ReceiptExtractEnrichmentService receiptExtractEnrichmentService,
        ILogger<MaintenanceController> logger)
    {
        _organizationRepository = organizationRepository;
        _organizationManager = organizationManager;
        _accountingRepository = accountingRepository;
        _accountingManager = accountingManager;
        _maintenanceManager = maintenanceManager;
        _maintenanceRepository = maintenanceRepository;
        _photoRepository = photoRepository;
        _propertyRepository = propertyRepository;
        _fileService = fileService;
        _fileAttachmentHelper = fileAttachmentHelper;
        _documentIntelligenceService = documentIntelligenceService;
        _receiptExtractEnrichmentService = receiptExtractEnrichmentService;
        _logger = logger;
    }
}

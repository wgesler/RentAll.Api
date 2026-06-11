using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/common")]
    [AllowAnonymous]
    public partial class CommonController : BaseController
    {
        private readonly AppSettings _appSettings;
        private readonly IDailyQuoteService _dailyQuoteService;
        private readonly ICalendarService _calendarService;
        private readonly ICalendarManager _calendarManager;
        private readonly ICommonRepository _commonRepository;
        private readonly ILeadRepository _leadRepository;
        private readonly IContactManager _contactManager;
        private readonly IContactRepository _contactRepository;
        private readonly IPropertyManager _propertyManager;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ILogger<CommonController> _logger;

        public CommonController(
            IOptions<AppSettings> options,
            IDailyQuoteService dailyQuoteService,
            ICalendarService calendarService,
            ICommonRepository commonRepository,
            ILeadRepository leadRepository,
            IContactManager contactManager,
            IContactRepository contactRepository,
            IPropertyManager propertyManager,
            IOrganizationRepository organizationRepository,
            ICalendarManager calendarManager,
            IPropertyRepository propertyRepository,
            IFileAttachmentHelper fileAttachmentHelper,
            IPdfGenerationService pdfGenerationService,
            ILogger<CommonController> logger)
        {
            _appSettings = options.Value;
            _dailyQuoteService = dailyQuoteService;
            _calendarService = calendarService;
            _commonRepository = commonRepository;
            _leadRepository = leadRepository;
            _contactManager = contactManager;
            _contactRepository = contactRepository;
            _propertyManager = propertyManager;
            _organizationRepository = organizationRepository;
            _calendarManager = calendarManager;
            _propertyRepository = propertyRepository;
            _fileAttachmentHelper = fileAttachmentHelper;
            _pdfGenerationService = pdfGenerationService;
            _logger = logger;
        }
    }
}

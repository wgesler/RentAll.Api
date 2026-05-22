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
        private readonly ICalendarManager _calendarManager;
        private readonly ICommonRepository _commonRepository;
        private readonly ILeadRepository _leadRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly ILogger<CommonController> _logger;

        public CommonController(
            IOptions<AppSettings> options,
            IDailyQuoteService dailyQuoteService,
            ICommonRepository commonRepository,
            ILeadRepository leadRepository,
            IOrganizationRepository organizationRepository,
            ICalendarManager calendarManager,
            IPropertyRepository propertyRepository,
            IFileAttachmentHelper fileAttachmentHelper,
            ILogger<CommonController> logger)
        {
            _appSettings = options.Value;
            _dailyQuoteService = dailyQuoteService;
            _commonRepository = commonRepository;
            _leadRepository = leadRepository;
            _organizationRepository = organizationRepository;
            _calendarManager = calendarManager;
            _propertyRepository = propertyRepository;
            _fileAttachmentHelper = fileAttachmentHelper;
            _logger = logger;
        }
    }
}

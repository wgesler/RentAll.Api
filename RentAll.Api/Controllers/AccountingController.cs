using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using System.Collections.Concurrent;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/accounting")]
    [Authorize]
    public partial class AccountingController : BaseController
    {
        private static readonly ConcurrentDictionary<string, JournalEntrySyncJobState> SyncJobs = new();

        private readonly IAccountingRepository _accountingRepository;
        private readonly IJournalEntryRepository _journalEntryRepository;
        private readonly IMaintenanceRepository _maintenanceRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationManager _organizationManager;
        private readonly IAccountingManager _accountingManager;
        private readonly IFileService _fileService;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly AuthManager _authManager;
        private readonly ILogger<AccountingController> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AccountingController(
            IAccountingRepository accountingRepository,
            IJournalEntryRepository journalEntryRepository,
            IMaintenanceRepository maintenanceRepository,
            IReservationRepository reservationRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationManager organizationManager,
            IAccountingManager accountingManager,
            IFileService fileService,
            IFileAttachmentHelper fileAttachmentHelper,
            AuthManager authManager,
            ILogger<AccountingController> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _accountingRepository = accountingRepository;
            _journalEntryRepository = journalEntryRepository;
            _maintenanceRepository = maintenanceRepository;
            _reservationRepository = reservationRepository;
            _organizationRepository = organizationRepository;
            _organizationManager = organizationManager;
            _accountingManager = accountingManager;
            _fileService = fileService;
            _fileAttachmentHelper = fileAttachmentHelper;
            _authManager = authManager;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        private sealed class JournalEntrySyncJobState
        {
            public required string JobId { get; init; }
            public bool IsRunning { get; set; }
            public bool IsCompleted { get; set; }
            public string? Message { get; set; }
            public Dictionary<string, JournalEntrySyncJobTypeStatusDto> Types { get; } = new(StringComparer.OrdinalIgnoreCase);
            public object SyncRoot { get; } = new();
        }
    }
}

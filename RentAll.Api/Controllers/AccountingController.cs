using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

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
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IAccountingManager _accountingManager;
        private readonly ILogger<AccountingController> _logger;

        public AccountingController(
            IAccountingRepository accountingRepository,
            IJournalEntryRepository journalEntryRepository,
            IReservationRepository reservationRepository,
            IOrganizationRepository organizationRepository,
            IAccountingManager accountingManager,
            ILogger<AccountingController> logger)
        {
            _accountingRepository = accountingRepository;
            _journalEntryRepository = journalEntryRepository;
            _reservationRepository = reservationRepository;
            _organizationRepository = organizationRepository;
            _accountingManager = accountingManager;
            _logger = logger;
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

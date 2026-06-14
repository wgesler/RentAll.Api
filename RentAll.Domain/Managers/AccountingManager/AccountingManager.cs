using RentAll.Domain.Enums;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Domain.Managers;

public partial class AccountingManager : IAccountingManager
{
    Guid SystemOrganization = Guid.Parse("99999999-9999-9999-9999-999999999999");


    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IOrganizationManager _organizationManager;
    private readonly IFeatureFlagService _featureFlagService;

    public AccountingManager(
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository,
        IAccountingRepository accountingRepository,
        IMaintenanceRepository maintenanceRepository,
        IReservationRepository reservationRepository,
        IJournalEntryRepository journalEntryRepository,
        IOrganizationManager organizationManager,
        IFeatureFlagService featureFlagService)
    {
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
        _accountingRepository = accountingRepository;
        _maintenanceRepository = maintenanceRepository;
        _reservationRepository = reservationRepository;
        _journalEntryRepository = journalEntryRepository;
        _organizationManager = organizationManager;
        _featureFlagService = featureFlagService;
    }

    bool IsAccountingFeatureEnabled()
        => _featureFlagService.IsEnabled(FeatureFlagKeys.Accounting);
}

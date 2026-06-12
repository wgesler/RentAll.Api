using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

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

    public AccountingManager(
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository,
        IAccountingRepository accountingRepository,
        IMaintenanceRepository maintenanceRepository,
        IReservationRepository reservationRepository,
        IJournalEntryRepository journalEntryRepository)
    {
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
        _accountingRepository = accountingRepository;
        _maintenanceRepository = maintenanceRepository;
        _reservationRepository = reservationRepository;
        _journalEntryRepository = journalEntryRepository;
    }
}

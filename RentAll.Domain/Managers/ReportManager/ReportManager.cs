using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Domain.Managers;

public partial class ReportManager : IReportManager
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPropertyRepository _propertyRepository;

    public ReportManager(
        IJournalEntryRepository journalEntryRepository,
        IAccountingRepository accountingRepository,
        IOrganizationRepository organizationRepository,
        IPropertyRepository propertyRepository)
    {
        _journalEntryRepository = journalEntryRepository;
        _accountingRepository = accountingRepository;
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
    }
}

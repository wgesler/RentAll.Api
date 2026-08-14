using Moq;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class AccountingManagerReservationBillingDateTests
{
    [Fact]
    public void GetLedgerLinesByReservationIdAsync_UsesBillingStartAndEndInsteadOfStayDates()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 7, 21),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        reservation.BillingStartDate = new DateOnly(2026, 8, 1);
        reservation.BillingEndDate = new DateOnly(2026, 9, 30);

        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();

        var julyLines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            AccountingManagerJournalEntryTestSupport.RentalCostCodeId);
        var augustLines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            AccountingManagerJournalEntryTestSupport.RentalCostCodeId);
        var octoberLines = manager.GetLedgerLinesByReservationIdAsync(
            reservation,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31),
            AccountingManagerJournalEntryTestSupport.RentalCostCodeId);

        Assert.DoesNotContain(julyLines, line => line.Description.StartsWith("Rental Fee", StringComparison.Ordinal));
        Assert.Contains(augustLines, line => line.Description.StartsWith("Rental Fee", StringComparison.Ordinal));
        Assert.DoesNotContain(octoberLines, line => line.Description.StartsWith("Rental Fee", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetReservationInvoicePreviewsAsync_UsesBillingStartAndEndForBillableMonths()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 7, 21),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        reservation.ReservationCode = "R-BILLING-WINDOW";
        reservation.CurrentInvoiceNo = 0;
        reservation.OfficeName = "Test Office";
        reservation.BillingStartDate = new DateOnly(2026, 8, 1);
        reservation.BillingEndDate = new DateOnly(2026, 9, 30);

        var manager = CreatePreviewManager(reservation);

        var previews = await manager.GetReservationInvoicePreviewsAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            reservation.ReservationId);

        Assert.Equal(2, previews.Count);
        Assert.Equal(new DateOnly(2026, 8, 1), previews[0].AccountingPeriod);
        Assert.Equal(new DateOnly(2026, 9, 1), previews[1].AccountingPeriod);
    }

    private static AccountingManager CreatePreviewManager(Reservation reservation)
    {
        var accountingRepository = new Mock<IAccountingRepository>();
        accountingRepository
            .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
            .ReturnsAsync([]);

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(r => r.GetOfficeByIdAsync(AccountingManagerJournalEntryTestSupport.OfficeId, AccountingManagerJournalEntryTestSupport.OrganizationId))
            .ReturnsAsync(new Office
            {
                OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                FurnishedRentChargeCcId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
                UnfurnishedRentChargeCcId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId
            });
        organizationRepository
            .Setup(r => r.GetAccountingOfficeByIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
            .ReturnsAsync(new AccountingOffice
            {
                OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                StartMonth = 1,
                StartYear = 2026
            });

        var propertyRepository = new Mock<IPropertyRepository>();
        propertyRepository
            .Setup(r => r.GetPropertyByIdAsync(AccountingManagerJournalEntryTestSupport.PropertyId, AccountingManagerJournalEntryTestSupport.OrganizationId))
            .ReturnsAsync(new Property
            {
                PropertyId = AccountingManagerJournalEntryTestSupport.PropertyId,
                OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                Unfurnished = false
            });

        var reservationRepository = new Mock<IReservationRepository>();
        reservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.ReservationId, AccountingManagerJournalEntryTestSupport.OrganizationId))
            .ReturnsAsync(reservation);

        return new AccountingManager(
            organizationRepository.Object,
            propertyRepository.Object,
            accountingRepository.Object,
            maintenanceRepository: null!,
            reservationRepository.Object,
            journalEntryRepository: null!,
            organizationManager: null!,
            contactRepository: null!,
            featureFlagService: new EnabledFeatureFlagService());
    }

    private sealed class EnabledFeatureFlagService : IFeatureFlagService
    {
        public IReadOnlyDictionary<string, bool> GetAll()
            => new Dictionary<string, bool> { [FeatureFlagKeys.Accounting] = true };

        public bool IsEnabled(string featureName) => true;

        public Task<bool> IsEnabledAsync(string featureName, Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public void Set(string featureName, bool enabled)
        {
        }
    }
}

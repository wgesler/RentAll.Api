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
            reservation.ReservationId,
            AccountingManagerJournalEntryTestSupport.CurrentUser);

        var currentMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var expectedCount = 0;
        if (new DateOnly(2026, 8, 1) >= currentMonth)
            expectedCount++;
        if (new DateOnly(2026, 9, 1) >= currentMonth)
            expectedCount++;

        Assert.Equal(expectedCount, previews.Count);
        Assert.All(previews, preview => Assert.True(preview.AccountingPeriod >= currentMonth));
    }

    private static AccountingManager CreatePreviewManager(Reservation reservation)
    {
        var accountingRepository = new Mock<IAccountingRepository>();
        accountingRepository
            .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
            .ReturnsAsync([]);
        accountingRepository
            .Setup(r => r.GetCostCodesByOfficeIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
            .ReturnsAsync([
                new CostCode
                {
                    CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    Code = "4000",
                    Description = "Rent",
                    TransactionType = TransactionType.ChargeProrate,
                    IsActive = true
                }
            ]);
        accountingRepository
            .Setup(r => r.GetChartOfAccountsByOfficeIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
            .ReturnsAsync([]);
        accountingRepository
            .Setup(r => r.GetBankCardsByOfficeIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
            .ReturnsAsync([]);
        var billedRowsByReservationId = new Dictionary<Guid, List<Billed>>();
        accountingRepository
            .Setup(r => r.ReplaceBilledMonthlyRowsForReservationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<Billed>>()))
            .Callback<Guid, Guid, IReadOnlyList<Billed>>((_, reservationId, rows) =>
            {
                billedRowsByReservationId[reservationId] = rows.ToList();
            })
            .Returns(Task.CompletedTask);
        accountingRepository
            .Setup(r => r.DeleteBilledByOrganizationAndOfficeIdsExceptReservationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(Task.CompletedTask);
        accountingRepository
            .Setup(r => r.GetBilledByReservationIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Guid _, Guid reservationId) =>
                billedRowsByReservationId.TryGetValue(reservationId, out var rows)
                    ? rows
                    : []);

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
                StartYear = 2026,
                InvoiceStartMonth = 6,
                InvoiceStartYear = 2026
            });
        organizationRepository
            .Setup(r => r.GetAccountingOfficesByOfficeIdsAsync(
                AccountingManagerJournalEntryTestSupport.OrganizationId,
                It.IsAny<string>()))
            .ReturnsAsync([
                new AccountingOffice
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    StartMonth = 1,
                    StartYear = 2026,
                    InvoiceStartMonth = 6,
                    InvoiceStartYear = 2026
                }
            ]);

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
        reservationRepository
            .Setup(r => r.GetActiveReservationsByOfficeIdsAsync(
                AccountingManagerJournalEntryTestSupport.OrganizationId,
                AccountingManagerJournalEntryTestSupport.OfficeId.ToString()))
            .ReturnsAsync([reservation]);

        return new AccountingManager(
            organizationRepository.Object,
            propertyRepository.Object,
            accountingRepository.Object,
            maintenanceRepository: null!,
            reservationRepository.Object,
            journalEntryRepository: null!,
            organizationManager: null!,
            contactRepository: null!,
            featureFlagService: new EnabledFeatureFlagService(),
            healthRepository: null!);
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

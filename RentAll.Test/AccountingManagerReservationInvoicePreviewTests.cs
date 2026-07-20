using Moq;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class AccountingManagerReservationInvoicePreviewTests
{
    [Theory]
    [InlineData(ProrateType.FirstMonth)]
    [InlineData(ProrateType.SecondMonth)]
    public void PreviewAllScenario_7_21_2026_Through_10_31_2026_UsesMonthBoundariesForLedgerLines(ProrateType prorateType)
    {
        var arrival = new DateOnly(2026, 7, 21);
        var departure = new DateOnly(2026, 10, 31);
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            arrival,
            departure,
            prorateType,
            BillingType.Monthly,
            3000m);
        reservation.ReservationCode = "R-TEST";
        reservation.CurrentInvoiceNo = 0;

        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var billingMonths = new[]
        {
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 1)
        };

        var monthBoundaryCount = 0;

        foreach (var billingMonth in billingMonths)
        {
            var monthStart = billingMonth;
            var monthEnd = new DateOnly(billingMonth.Year, billingMonth.Month, DateTime.DaysInMonth(billingMonth.Year, billingMonth.Month));

            var monthLines = manager.GetLedgerLinesByReservationIdAsync(reservation, monthStart, monthEnd, 77);
            if (monthLines.Count > 0 && monthLines.Sum(line => line.Amount) != 0)
                monthBoundaryCount++;
        }

        Assert.Equal(4, monthBoundaryCount);
    }

    [Fact]
    public async Task GetReservationInvoicePreviewsAsync_ReturnsPreviewsWhenBillingRateIsZero()
    {
        var reservation = CreatePreviewReservation();
        reservation.BillingRate = 0m;
        var manager = CreatePreviewManager(reservation);

        var previews = await manager.GetReservationInvoicePreviewsAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            reservation.ReservationId);

        Assert.Equal(4, previews.Count);
        Assert.All(previews, preview => Assert.Equal(0m, preview.TotalAmount));
    }

    [Fact]
    public async Task GetReservationInvoicePreviewsAsync_ReturnsEveryBillableMonthThroughDeparture()
    {
        var reservation = CreatePreviewReservation();
        var manager = CreatePreviewManager(reservation);

        var previews = await manager.GetReservationInvoicePreviewsAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            reservation.ReservationId);

        Assert.Equal(4, previews.Count);
        Assert.Equal(new DateOnly(2026, 7, 1), previews[0].AccountingPeriod);
        Assert.Equal(new DateOnly(2026, 8, 1), previews[1].AccountingPeriod);
        Assert.Equal(new DateOnly(2026, 9, 1), previews[2].AccountingPeriod);
        Assert.Equal(new DateOnly(2026, 10, 1), previews[3].AccountingPeriod);
    }

    [Fact]
    public async Task GetPreBillingInvoicesAsync_ReturnsOnlySelectedBillingMonth()
    {
        var reservation = CreatePreviewReservation();
        var manager = CreatePreviewManager(
            reservation,
            configureReservationRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveReservationsByOfficeIdsAsync(
                        AccountingManagerJournalEntryTestSupport.OrganizationId,
                        AccountingManagerJournalEntryTestSupport.OfficeId.ToString()))
                    .ReturnsAsync([reservation]);
            },
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveInvoicesByAccountingMonthAsync(It.IsAny<ActiveInvoiceByAccountingMonthCriteria>()))
                    .ReturnsAsync([]);
            });

        var augustOnly = await manager.GetPreBillingInvoicesAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString(),
            new DateOnly(2026, 8, 1));

        var julyOnly = await manager.GetPreBillingInvoicesAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString(),
            new DateOnly(2026, 7, 1));

        Assert.Single(augustOnly);
        Assert.Equal(new DateOnly(2026, 8, 1), augustOnly[0].AccountingPeriod);
        Assert.Single(julyOnly);
        Assert.Equal(new DateOnly(2026, 7, 1), julyOnly[0].AccountingPeriod);
    }

    [Fact]
    public async Task GetPreBillingInvoicesAsync_SkipsReservationsWithFullyPaidInvoiceForMonth()
    {
        var reservation = CreatePreviewReservation();
        var existingAugustInvoice = CreateExistingInvoice(reservation, "R-PREVIEW-002", new DateOnly(2026, 8, 1));
        existingAugustInvoice.TotalAmount = 3000m;
        existingAugustInvoice.PaidAmount = 3000m;

        var manager = CreatePreviewManager(
            reservation,
            configureReservationRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveReservationsByOfficeIdsAsync(
                        AccountingManagerJournalEntryTestSupport.OrganizationId,
                        AccountingManagerJournalEntryTestSupport.OfficeId.ToString()))
                    .ReturnsAsync([reservation]);
            },
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveInvoicesByAccountingMonthAsync(It.IsAny<ActiveInvoiceByAccountingMonthCriteria>()))
                    .ReturnsAsync([existingAugustInvoice]);
            });

        var augustPreviews = await manager.GetPreBillingInvoicesAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString(),
            new DateOnly(2026, 8, 1));

        Assert.Empty(augustPreviews);
    }

    [Fact]
    public async Task GetReservationInvoicePreviewsAsync_LoadsExistingInvoicesWithIncludePaid()
    {
        var reservation = CreatePreviewReservation();
        InvoiceGetCriteria? capturedCriteria = null;

        var manager = CreatePreviewManager(
            reservation,
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
                    .Callback<InvoiceGetCriteria>(criteria => capturedCriteria = criteria)
                    .ReturnsAsync([]);
            });

        await manager.GetReservationInvoicePreviewsAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            reservation.ReservationId);

        Assert.NotNull(capturedCriteria);
        Assert.True(capturedCriteria!.IncludePaid);
    }

    [Fact]
    public async Task GetReservationInvoicePreviewsAsync_AssignsNextAvailableInvoiceCodes()
    {
        var reservation = CreatePreviewReservation();
        reservation.CurrentInvoiceNo = 4;
        var existingInvoices = new[]
        {
            CreateExistingInvoice(reservation, "R-PREVIEW-001", new DateOnly(2026, 7, 1)),
            CreateExistingInvoice(reservation, "R-PREVIEW-002", new DateOnly(2026, 8, 1)),
            CreateExistingInvoice(reservation, "R-PREVIEW-003", new DateOnly(2026, 9, 1)),
            CreateExistingInvoice(reservation, "R-PREVIEW-004", new DateOnly(2026, 10, 1))
        };

        var manager = CreatePreviewManager(
            reservation,
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
                    .ReturnsAsync(existingInvoices);
            });

        var previews = await manager.GetReservationInvoicePreviewsAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            reservation.ReservationId);

        Assert.Empty(previews);
    }

    [Fact]
    public async Task GetReservationInvoicePreviewsAsync_SkipsUsedCodesWhenMonthsAreMissing()
    {
        var reservation = CreatePreviewReservation();
        reservation.CurrentInvoiceNo = 4;
        var existingInvoices = new[]
        {
            CreateExistingInvoice(reservation, "R-PREVIEW-001", new DateOnly(2026, 7, 1)),
            CreateExistingInvoice(reservation, "R-PREVIEW-002", new DateOnly(2026, 8, 1)),
            CreateExistingInvoice(reservation, "R-PREVIEW-004", new DateOnly(2026, 10, 1))
        };

        var manager = CreatePreviewManager(
            reservation,
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
                    .ReturnsAsync(existingInvoices);
            });

        var previews = await manager.GetReservationInvoicePreviewsAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            reservation.ReservationId);

        Assert.Single(previews);
        Assert.Equal(new DateOnly(2026, 9, 1), previews[0].AccountingPeriod);
        Assert.Equal("R-PREVIEW-005", previews[0].InvoiceCode);
    }

    [Fact]
    public async Task GetMissingInvoicesAsync_LoadsExistingInvoicesWithIncludePaid()
    {
        var reservation = CreatePreviewReservation();
        InvoiceGetCriteria? capturedCriteria = null;

        var manager = CreatePreviewManager(
            reservation,
            configureReservationRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveReservationsByOfficeIdsAsync(
                        AccountingManagerJournalEntryTestSupport.OrganizationId,
                        AccountingManagerJournalEntryTestSupport.OfficeId.ToString()))
                    .ReturnsAsync([reservation]);
            },
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
                    .Callback<InvoiceGetCriteria>(criteria => capturedCriteria = criteria)
                    .ReturnsAsync([]);
            });

        await manager.GetMissingInvoicesAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString());

        Assert.NotNull(capturedCriteria);
        Assert.True(capturedCriteria!.IncludePaid);
    }

    [Fact]
    public async Task GetMissingInvoicesAsync_SkipsMonthsWithFullyPaidExistingInvoices()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        reservation.ReservationCode = "R-PREVIEW-PAID";
        reservation.CurrentInvoiceNo = 1;
        reservation.OfficeName = "Test Office";
        var existingInvoices = new[]
        {
            CreateExistingInvoice(reservation, "R-PREVIEW-PAID-001", new DateOnly(2026, 1, 1))
        };
        existingInvoices[0].TotalAmount = 3000m;
        existingInvoices[0].PaidAmount = 3000m;

        var manager = CreatePreviewManager(
            reservation,
            configureReservationRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveReservationsByOfficeIdsAsync(
                        AccountingManagerJournalEntryTestSupport.OrganizationId,
                        AccountingManagerJournalEntryTestSupport.OfficeId.ToString()))
                    .ReturnsAsync([reservation]);
            },
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
                    .ReturnsAsync((InvoiceGetCriteria criteria) =>
                        criteria.IncludePaid ? existingInvoices : []);
            });

        var previews = await manager.GetMissingInvoicesAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString());

        Assert.DoesNotContain(previews, preview => preview.AccountingPeriod == new DateOnly(2026, 1, 1));
        Assert.Equal(2, previews.Count);
        Assert.Contains(previews, preview => preview.AccountingPeriod == new DateOnly(2026, 2, 1));
        Assert.Contains(previews, preview => preview.AccountingPeriod == new DateOnly(2026, 3, 1));
    }

    [Fact]
    public async Task GetMissingInvoicesAsync_DoesNotScanMonthsBeforeAccountingStart()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2018, 3, 1),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        reservation.ReservationCode = "R-OLD";
        reservation.CurrentInvoiceNo = 0;
        reservation.OfficeName = "Test Office";

        var manager = CreatePreviewManager(
            reservation,
            configureReservationRepository: repo =>
            {
                repo
                    .Setup(r => r.GetActiveReservationsByOfficeIdsAsync(
                        AccountingManagerJournalEntryTestSupport.OrganizationId,
                        AccountingManagerJournalEntryTestSupport.OfficeId.ToString()))
                    .ReturnsAsync([reservation]);
            },
            configureAccountingRepository: repo =>
            {
                repo
                    .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
                    .ReturnsAsync([]);
            });

        var previews = await manager.GetMissingInvoicesAsync(
            AccountingManagerJournalEntryTestSupport.OrganizationId,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString());

        Assert.NotEmpty(previews);
        Assert.All(previews, preview => Assert.True(preview.AccountingPeriod >= new DateOnly(2026, 1, 1)));
        Assert.DoesNotContain(previews, preview => preview.AccountingPeriod < new DateOnly(2026, 1, 1));
    }

    private static Invoice CreateExistingInvoice(Reservation reservation, string invoiceCode, DateOnly accountingPeriod)
        => new()
        {
            InvoiceId = Guid.NewGuid(),
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            ReservationId = reservation.ReservationId,
            ReservationCode = reservation.ReservationCode,
            InvoiceCode = invoiceCode,
            AccountingPeriod = accountingPeriod,
            IsActive = true
        };

    private static Reservation CreatePreviewReservation()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 7, 21),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        reservation.ReservationCode = "R-PREVIEW";
        reservation.CurrentInvoiceNo = 0;
        reservation.OfficeName = "Test Office";
        return reservation;
    }

    private static AccountingManager CreatePreviewManager(
        Reservation reservation,
        Action<Mock<IReservationRepository>>? configureReservationRepository = null,
        Action<Mock<IAccountingRepository>>? configureAccountingRepository = null)
    {
        var accountingRepository = new Mock<IAccountingRepository>();
        accountingRepository
            .Setup(r => r.GetInvoicesAsync(It.IsAny<InvoiceGetCriteria>()))
            .ReturnsAsync([]);
        configureAccountingRepository?.Invoke(accountingRepository);

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
        configureReservationRepository?.Invoke(reservationRepository);

        return new AccountingManager(
            organizationRepository.Object,
            propertyRepository.Object,
            accountingRepository.Object,
            maintenanceRepository: null!,
            reservationRepository.Object,
            journalEntryRepository: null!,
            organizationManager: null!,
            contactRepository: null!,
            new EnabledFeatureFlagService());
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

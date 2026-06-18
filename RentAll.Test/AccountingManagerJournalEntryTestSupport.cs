using System.Text.RegularExpressions;
using Moq;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

internal static class AccountingManagerJournalEntryTestSupport
{
    internal const int OfficeId = 1;
    internal const int RentalCostCodeId = 77;
    internal const int AccountsReceivableAccountId = 100;
    internal const int TenantIncomeAccountId = 200;

    internal static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid ReservationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    internal static readonly Guid PropertyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    internal static readonly Guid CurrentUser = Guid.Parse("22222222-2222-2222-2222-222222222222");

    internal static AccountingManager CreateLedgerLineManager()
        => new(
            organizationRepository: null!,
            propertyRepository: null!,
            accountingRepository: null!,
            maintenanceRepository: null!,
            reservationRepository: null!,
            journalEntryRepository: null!,
            organizationManager: null!,
            featureFlagService: new EnabledFeatureFlagService());

    internal static JournalEntryTestContext CreateJournalEntryTestContext(Reservation reservation)
        => new(reservation);

    internal static Reservation CreateReservation(
        DateOnly arrival,
        DateOnly departure,
        ProrateType prorateType,
        BillingType billingType,
        decimal billingRate)
    {
        return new Reservation
        {
            ReservationId = ReservationId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ArrivalDate = arrival,
            DepartureDate = departure,
            BillingType = billingType,
            ProrateType = prorateType,
            BillingRate = billingRate,
            DepositType = DepositType.CLR,
            DepartureFee = -1m,
            HasPets = false,
            MaidStartDate = new DateOnly(2100, 1, 1),
            Frequency = FrequencyType.Monthly,
            ExtraFeeLines = []
        };
    }

    internal static decimal GetBillingRate(BillingType billingType)
        => billingType == BillingType.Monthly ? 3000m : 100m;

    internal static Invoice BuildInvoice(Reservation reservation, DateOnly periodStart, DateOnly periodEnd, List<LedgerLine> ledgerLines)
    {
        var invoiceId = Guid.NewGuid();
        var lineNumber = 1;
        foreach (var line in ledgerLines)
        {
            line.LedgerLineId = Guid.NewGuid();
            line.InvoiceId = invoiceId;
            line.LineNumber = lineNumber++;
            line.ReservationId ??= reservation.ReservationId;
            if (line.CostCodeId == 0)
                line.CostCodeId = RentalCostCodeId;
        }

        return new Invoice
        {
            InvoiceId = invoiceId,
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            InvoiceCode = "INV-TEST",
            ReservationId = reservation.ReservationId,
            PropertyId = reservation.PropertyId,
            AccountingPeriod = new DateOnly(periodStart.Year, periodStart.Month, 1),
            InvoiceDate = periodStart,
            InvoicePeriod = $"{periodStart:MM/dd/yyyy} - {periodEnd:MM/dd/yyyy}",
            TotalAmount = ledgerLines.Sum(l => l.Amount),
            LedgerLines = ledgerLines
        };
    }

    internal static void AssertJournalEntriesBalanceInvoice(
        IReadOnlyList<JournalEntry> journalEntries,
        Invoice invoice,
        string? caseId = null)
    {
        Assert.NotEmpty(journalEntries);

        var expectedChargeTotal = invoice.LedgerLines
            .Where(l => l.Amount != 0)
            .Sum(l => l.Amount);

        decimal totalAccountsReceivableDebit = 0;
        decimal totalIncomeCredit = 0;

        foreach (var entry in journalEntries)
        {
            var activeLines = entry.JournalEntryLines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
            var totalDebit = activeLines.Sum(l => l.Debit);
            var totalCredit = activeLines.Sum(l => l.Credit);
            Assert.Equal(totalDebit, totalCredit);

            totalAccountsReceivableDebit += activeLines
                .Where(l => l.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
                .Sum(l => l.Debit);

            totalIncomeCredit += activeLines
                .Where(l => !l.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
                .Sum(l => l.Credit);
        }

        var caseLabel = string.IsNullOrWhiteSpace(caseId) ? string.Empty : $"Case {caseId}: ";
        Assert.True(
            expectedChargeTotal == totalAccountsReceivableDebit && expectedChargeTotal == totalIncomeCredit,
            $"{caseLabel}Journal entry totals ({totalAccountsReceivableDebit} A/R, {totalIncomeCredit} income) must equal invoice charges ({expectedChargeTotal}).");
    }

    internal enum JournalEntryInvoicePath
    {
        StandardSingleJe,
        CrossPeriodSplit,
        CrossPeriodFallback
    }

    internal static bool InvoiceCrossesAccountingPeriod(Invoice invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoicePeriod))
            return false;

        var periodParts = invoice.InvoicePeriod.Split('-', StringSplitOptions.TrimEntries);
        var periodStart = DateOnly.Parse(periodParts[0]);
        var periodEnd = DateOnly.Parse(periodParts[1]);
        if (periodStart.Year != periodEnd.Year || periodStart.Month != periodEnd.Month)
            return true;

        foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
        {
            var match = RentalFeePeriodRegex.Match(line.Description.Trim());
            if (!match.Success)
                continue;

            if (match.Groups["start"].Value[..2] != match.Groups["end"].Value[..2])
                return true;
        }

        return false;
    }

    internal static JournalEntryInvoicePath ClassifyJournalEntryPath(Invoice invoice, int journalEntryCount)
    {
        var crosses = InvoiceCrossesAccountingPeriod(invoice);
        return (crosses, journalEntryCount) switch
        {
            (false, 1) => JournalEntryInvoicePath.StandardSingleJe,
            (true, 2) => JournalEntryInvoicePath.CrossPeriodSplit,
            (true, 1) => JournalEntryInvoicePath.CrossPeriodFallback,
            _ => throw new InvalidOperationException(
                $"Unexpected journal entry path: crosses={crosses}, journalEntryCount={journalEntryCount}")
        };
    }

    internal static void PrintJournalEntryPath(
        string caseLabel,
        Invoice invoice,
        IReadOnlyList<JournalEntry> journalEntries,
        string rentalDescription,
        decimal rentalAmount)
    {
        var path = ClassifyJournalEntryPath(invoice, journalEntries.Count);
        var pathLabel = path switch
        {
            JournalEntryInvoicePath.StandardSingleJe => "Standard",
            JournalEntryInvoicePath.CrossPeriodSplit => "CrossPeriodSplit",
            JournalEntryInvoicePath.CrossPeriodFallback => "CrossPeriodFallback",
            _ => path.ToString()
        };

        var arAmounts = journalEntries
            .Select(entry => entry.JournalEntryLines
                .Where(line => line.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
                .Sum(line => line.Debit))
            .ToList();

        var arSummary = arAmounts.Count switch
        {
            1 => $"A/R ${arAmounts[0]:0.##}",
            2 => $"A/R ${arAmounts[0]:0.##} + ${arAmounts[1]:0.##}",
            _ => string.Join(" + ", arAmounts.Select(amount => $"${amount:0.##}"))
        };

        Console.WriteLine(
            $"[{pathLabel}] {caseLabel} | {rentalDescription} ${rentalAmount:0.##} | {journalEntries.Count} JE | {arSummary}");
    }

    private static readonly Regex RentalFeePeriodRegex = new(
        @"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal sealed class JournalEntryTestContext
    {
        private readonly Reservation _reservation;
        private readonly List<JournalEntry> _createdJournalEntries = [];
        private int _journalEntryCodeSequence;

        internal JournalEntryTestContext(Reservation reservation)
        {
            _reservation = reservation;
        }

        internal IReadOnlyList<JournalEntry> CreatedJournalEntries => _createdJournalEntries;

        internal AccountingManager CreateManager()
        {
            var chartOfAccounts = new List<ChartOfAccount>
            {
                new()
                {
                    OrganizationId = OrganizationId,
                    OfficeId = OfficeId,
                    AccountId = AccountsReceivableAccountId,
                    AccountType = AccountType.AccountsReceivable,
                    Name = "Accounts Receivable",
                    AccountNo = "1200"
                },
                new()
                {
                    OrganizationId = OrganizationId,
                    OfficeId = OfficeId,
                    AccountId = TenantIncomeAccountId,
                    AccountType = AccountType.Income,
                    Name = "Tenant Income",
                    AccountNo = "4000"
                }
            };

            var costCodes = new List<CostCode>
            {
                new()
                {
                    CostCodeId = RentalCostCodeId,
                    OrganizationId = OrganizationId,
                    OfficeId = OfficeId,
                    Code = "4000",
                    Description = "Rent",
                    TransactionType = TransactionType.Charge,
                    IsActive = true
                }
            };

            var accountingRepository = new Mock<IAccountingRepository>();
            accountingRepository
                .Setup(r => r.GetCostCodesByOfficeIdAsync(OrganizationId, OfficeId))
                .ReturnsAsync(costCodes);
            accountingRepository
                .Setup(r => r.GetChartOfAccountsByOfficeIdAsync(OrganizationId, OfficeId))
                .ReturnsAsync(chartOfAccounts);
            accountingRepository
                .Setup(r => r.GetBankCardsByOfficeIdAsync(OrganizationId, OfficeId))
                .ReturnsAsync([]);

            var organizationRepository = new Mock<IOrganizationRepository>();
            organizationRepository
                .Setup(r => r.GetAccountingOfficeByIdAsync(OrganizationId, OfficeId))
                .ReturnsAsync(new AccountingOffice
                {
                    OrganizationId = OrganizationId,
                    OfficeId = OfficeId,
                    DefaultActRecvAccountId = AccountsReceivableAccountId,
                    DefaultTenantIncAccountId = TenantIncomeAccountId
                });
            organizationRepository
                .Setup(r => r.GetOfficeByIdAsync(OfficeId, OrganizationId))
                .ReturnsAsync(new Office
                {
                    OrganizationId = OrganizationId,
                    OfficeId = OfficeId,
                    FurnishedRentChargeCcId = RentalCostCodeId,
                    UnfurnishedRentChargeCcId = RentalCostCodeId
                });

            var propertyRepository = new Mock<IPropertyRepository>();
            propertyRepository
                .Setup(r => r.GetPropertyByIdAsync(PropertyId, OrganizationId))
                .ReturnsAsync(new Property
                {
                    PropertyId = PropertyId,
                    OrganizationId = OrganizationId,
                    Unfurnished = false
                });

            var journalEntryRepository = new Mock<IJournalEntryRepository>();
            journalEntryRepository
                .Setup(r => r.GetJournalEntriesAsync(It.IsAny<JournalEntryGetCriteria>()))
                .ReturnsAsync([]);
            journalEntryRepository
                .Setup(r => r.ExistsByJournalEntryCodeAsync(It.IsAny<string>(), OrganizationId))
                .ReturnsAsync(false);
            journalEntryRepository
                .Setup(r => r.CreateJournalEntryAsync(It.IsAny<JournalEntry>()))
                .ReturnsAsync((JournalEntry entry) =>
                {
                    entry.JournalEntryId = Guid.NewGuid();
                    _createdJournalEntries.Add(entry);
                    return entry;
                });

            var reservationRepository = new Mock<IReservationRepository>();
            reservationRepository
                .Setup(r => r.GetReservationByIdAsync(_reservation.ReservationId, OrganizationId))
                .ReturnsAsync(_reservation);

            var organizationManager = new Mock<IOrganizationManager>();
            organizationManager
                .Setup(m => m.GenerateEntityCodeAsync(OrganizationId, EntityType.JournalEntry))
                .ReturnsAsync(() => $"JE-{Interlocked.Increment(ref _journalEntryCodeSequence):D4}");

            return new AccountingManager(
                organizationRepository.Object,
                propertyRepository.Object,
                accountingRepository.Object,
                maintenanceRepository: null!,
                reservationRepository.Object,
                journalEntryRepository.Object,
                organizationManager.Object,
                new EnabledFeatureFlagService());
        }
    }

    internal sealed class EnabledFeatureFlagService : IFeatureFlagService
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

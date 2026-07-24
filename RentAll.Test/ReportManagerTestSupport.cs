using Moq;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

internal static class ReportManagerTestSupport
{
    internal const int OfficeId = 1;
    internal const int OwnerAccountsPayableAccountId = 500;
    internal const string OfficeName = "BWA";
    internal const string PropertyCode = "BWA24";

    internal static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid PropertyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    internal static readonly Guid OwnerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    internal static readonly Guid ReservationId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    internal static ReportManagerTestContext CreateContext(IEnumerable<JournalEntryRecapLine> recapLines)
        => new(recapLines);

    internal static JournalEntryRecapLine RecapLine(
        string recapCategory,
        decimal amount,
        string sourceDocumentCode,
        DateOnly accountingPeriod,
        DateOnly transactionDate,
        string? description = null,
        Guid? sourceId = null,
        int? sourceTypeId = null,
        string? reservationCode = null,
        Guid? journalEntryLineId = null)
    {
        return new JournalEntryRecapLine
        {
            JournalEntryLineId = journalEntryLineId ?? Guid.NewGuid(),
            JournalEntryId = Guid.NewGuid(),
            JournalEntryCode = $"JE-{sourceDocumentCode}",
            TransactionDate = transactionDate,
            AccountingPeriod = accountingPeriod,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            PropertyCode = PropertyCode,
            ReservationId = ReservationId,
            ReservationCode = reservationCode ?? ResolveReservationCode(sourceDocumentCode),
            SourceTypeId = sourceTypeId,
            SourceId = sourceId ?? ResolveTestSourceId(sourceDocumentCode),
            SourceDocumentCode = sourceDocumentCode,
            Description = description ?? sourceDocumentCode,
            RecapCategory = recapCategory,
            Amount = amount
        };
    }

    internal static string ResolveReservationCode(string sourceDocumentCode)
    {
        var dashIndex = sourceDocumentCode.LastIndexOf('-');
        return dashIndex > 0 ? sourceDocumentCode[..dashIndex] : sourceDocumentCode;
    }

    private static readonly object TestSourceIdLock = new();
    private static readonly Dictionary<string, Guid> TestSourceIdByDocumentCode = new(StringComparer.OrdinalIgnoreCase);

    private static Guid? ResolveTestSourceId(string sourceDocumentCode)
    {
        var code = (sourceDocumentCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
            return null;

        lock (TestSourceIdLock)
        {
            if (!TestSourceIdByDocumentCode.TryGetValue(code, out var sourceId))
            {
                sourceId = Guid.NewGuid();
                TestSourceIdByDocumentCode[code] = sourceId;
            }

            return sourceId;
        }
    }

    internal static void AssertActivityLine(
        OwnerStatementPropertyActivityLine line,
        string expectedDocumentCode,
        decimal expectedInvoiced,
        decimal expectedPaid,
        decimal expectedExpenses = 0m)
    {
        Assert.Equal(expectedDocumentCode, line.DocumentCode);
        Assert.Equal(expectedInvoiced, line.ExpectedIncome);
        Assert.Equal(expectedPaid, line.ReceivedIncome);
        Assert.Equal(expectedExpenses, line.Expenses);
    }

    internal static void AssertNoNegativePrepaidActivityLines(IEnumerable<OwnerStatementPropertyActivityLine> lines)
    {
        Assert.All(lines, line => Assert.True(line.PrepaidIncome >= 0));
    }

    internal static OwnerStatementPropertyActivityLine? FindActivityLine(
        IEnumerable<OwnerStatementPropertyActivityLine> lines,
        string documentCode)
        => lines.FirstOrDefault(line => string.Equals(line.DocumentCode, documentCode, StringComparison.OrdinalIgnoreCase));

    internal sealed class ReportManagerTestContext
    {
        private readonly IReadOnlyList<JournalEntryRecapLine> _recapLines;
        private readonly ReportManager _manager;
        private readonly JournalEntryRecapGetCriteria _criteria;

        internal ReportManagerTestContext(IEnumerable<JournalEntryRecapLine> recapLines)
        {
            _recapLines = recapLines.ToList();
            _criteria = new JournalEntryRecapGetCriteria
            {
                OrganizationId = OrganizationId,
                OfficeIds = OfficeId.ToString(),
                PropertyId = PropertyId,
                IncludeUnposted = true
            };

            var journalEntryRepository = new Mock<IJournalEntryRepository>();
            journalEntryRepository
                .Setup(repository => repository.GetJournalEntryRecapLinesAsync(It.IsAny<JournalEntryRecapGetCriteria>()))
                .ReturnsAsync((JournalEntryRecapGetCriteria criteria) => FilterRecapLines(criteria));
            journalEntryRepository
                .Setup(repository => repository.GetOwnerReportBundleDataAsync(It.IsAny<JournalEntryRecapGetCriteria>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
                .ReturnsAsync((JournalEntryRecapGetCriteria criteria, DateOnly? _, DateOnly? __) => new OwnerReportBundleData
                {
                    RecapLines = FilterRecapLines(criteria).ToList(),
                    OwnerApLines = []
                });
            journalEntryRepository
                .Setup(repository => repository.GetEscrowReportDataAsync(It.IsAny<JournalEntryRecapGetCriteria>()))
                .ReturnsAsync((JournalEntryRecapGetCriteria criteria) => new EscrowReportBundleData
                {
                    RecapLines = FilterRecapLines(criteria).ToList(),
                    EscrowOfficeBalances = [],
                    EscrowPrepaidPropertyBalances = []
                });
            journalEntryRepository
                .Setup(repository => repository.GetEscrowPrepaidApplyJournalEntryLinesAsync(It.IsAny<JournalEntryRecapGetCriteria>()))
                .ReturnsAsync([]);
            journalEntryRepository
                .Setup(repository => repository.GetEscrowBankJournalEntryLinesAsync(It.IsAny<JournalEntryRecapGetCriteria>()))
                .ReturnsAsync([]);
            journalEntryRepository
                .Setup(repository => repository.GetJournalEntryLinesAsync(It.IsAny<JournalEntryLineGetCriteria>()))
                .ReturnsAsync([]);

            var propertyRepository = new Mock<IPropertyRepository>();
            propertyRepository
                .Setup(repository => repository.GetPropertyReportDataAsync(OrganizationId, OfficeId.ToString(), PropertyId))
                .ReturnsAsync([CreatePropertyReportData()]);

            var accountingRepository = new Mock<IAccountingRepository>();
            accountingRepository
                .Setup(repository => repository.GetChartOfAccountsByOfficeIdAsync(OrganizationId, OfficeId))
                .ReturnsAsync([]);

            var organizationRepository = new Mock<IOrganizationRepository>();
            organizationRepository
                .Setup(repository => repository.GetAccountingOfficeByIdAsync(OrganizationId, OfficeId))
                .ReturnsAsync(new AccountingOffice
                {
                    OrganizationId = OrganizationId,
                    OfficeId = OfficeId,
                    DefaultOwnActPayableAccountId = OwnerAccountsPayableAccountId
                });

            var accountingManager = new Mock<IAccountingManager>();
            accountingManager
                .Setup(manager => manager.GetDefaultOwnerAccountsPayable(It.IsAny<List<ChartOfAccount>>(), OfficeId, It.IsAny<AccountingOffice?>()))
                .Returns(OwnerAccountsPayableAccountId);

            _manager = new ReportManager(
                journalEntryRepository.Object,
                accountingRepository.Object,
                organizationRepository.Object,
                propertyRepository.Object,
                accountingManager.Object);
        }

        internal Task<OwnerAccrualReport> GetAccrualReportAsync(DateOnly startDate, DateOnly endDate)
        {
            _criteria.StartDate = startDate;
            _criteria.EndDate = endDate;
            return _manager.GetOwnerAccrualReportAsync(_criteria);
        }

        internal Task<OwnerCashReport> GetCashReportAsync(DateOnly startDate, DateOnly endDate)
        {
            _criteria.StartDate = startDate;
            _criteria.EndDate = endDate;
            return _manager.GetOwnerCashReportAsync(_criteria);
        }

        internal Task<RecapReport> GetRecapReportAsync(DateOnly startDate, DateOnly endDate)
        {
            _criteria.StartDate = startDate;
            _criteria.EndDate = endDate;
            return _manager.GetJournalEntryRecapReportAsync(_criteria);
        }

        private IEnumerable<JournalEntryRecapLine> FilterRecapLines(JournalEntryRecapGetCriteria criteria)
        {
            return _recapLines
                .Select(line => new JournalEntryRecapLine
                {
                    JournalEntryLineId = line.JournalEntryLineId,
                    JournalEntryId = line.JournalEntryId,
                    JournalEntryCode = line.JournalEntryCode,
                    TransactionDate = line.TransactionDate,
                    AccountingPeriod = line.AccountingPeriod,
                    OfficeId = line.OfficeId,
                    PropertyId = line.PropertyId,
                    PropertyCode = line.PropertyCode,
                    ReservationId = line.ReservationId,
                    ReservationCode = line.ReservationCode,
                    SourceTypeId = line.SourceTypeId,
                    SourceId = line.SourceId,
                    SourceDocumentCode = line.SourceDocumentCode,
                    Description = line.Description,
                    RecapCategory = line.RecapCategory,
                    Amount = line.Amount,
                    IsInDateRange = IsLineInReportRange(line, criteria.StartDate, criteria.EndDate)
                });
        }

        private static bool IsLineInReportRange(JournalEntryRecapLine line, DateOnly? startDate, DateOnly? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
                return true;

            return line.TransactionDate >= startDate.Value && line.TransactionDate <= endDate.Value;
        }

        private static PropertyReportData CreatePropertyReportData()
        {
            return new PropertyReportData
            {
                PropertyId = PropertyId,
                PropertyCode = PropertyCode,
                OfficeId = OfficeId,
                OfficeName = OfficeName,
                PropertyLeaseType = PropertyLeaseType.PropertyManagement,
                PrimaryOwnerId = OwnerId,
                WorkingCapitalBalance = 0m
            };
        }
    }
}
